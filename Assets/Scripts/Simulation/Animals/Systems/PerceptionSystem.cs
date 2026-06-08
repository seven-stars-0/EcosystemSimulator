using System.Collections.Generic;
using UnityEngine;

public static class PerceptionSystem
{
    private static readonly List<Animal>     _animalBuf = new(64);
    private static readonly List<Vector2Int> _plantBuf  = new(32);

    // ── dt aggiunto come parametro ────────────────────────────────────────────
    public static PerceptionData Compute(
        Animal              animal,
        AnimalState         state,
        WorldGrid           grid,
        RenderConfig        cfg,
        SpatialGrid<Animal> animalGrid,
        PlantManager        plantMgr,
        SimulationSettings  settings,
        float               dt)
    {
        var     p     = new PerceptionData();
        float   range = state.genes.visionRange;
        Vector2 pos   = state.position;

        // Wander usa dt scalato → si aggiorna alla stessa "velocità simulazione"
        p.wanderVector  = UpdateWander(state.wanderDir, dt);
        state.wanderDir = p.wanderVector;

        ComputeSlope(pos, grid, cfg, ref p);

        animalGrid.Query(pos, range, _animalBuf);

        ComputeSocialFleeAndSeparation(animal, state, pos, range, _animalBuf, settings, ref p);

        if (state.CanMate(settings))
            ComputeMate(animal, state, pos, range, _animalBuf, settings, ref p);

        if (state.species == AnimalSpecies.Prey)
            ComputeFoodPrey(pos, range, grid, cfg, plantMgr, ref p);
        else
            ComputeFoodPredator(animal, state, pos, range, _animalBuf, ref p);

        ComputeWater(pos, range, grid, cfg, ref p);

        return p;
    }

    // ── Wander ────────────────────────────────────────────────────────────────

    private static Vector2 UpdateWander(Vector2 current, float dt)
    {
        // 120°/s di variazione casuale, ora proporzionale al dt scalato.
        // A timeScale=5 il wander cambia 5x più velocemente → esplorazione proporzionale.
        float angle    = Mathf.Atan2(current.y, current.x);
        float noise    = Random.Range(-120f, 120f) * Mathf.Deg2Rad * dt;
        float newAngle = angle + noise;
        return new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
    }

    // ── Il resto dei metodi è identico alla versione precedente ──────────────

    private static void ComputeSlope(Vector2 pos, WorldGrid grid, RenderConfig cfg,
                                     ref PerceptionData p)
    {
        int cx = Mathf.RoundToInt(pos.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / cfg.cellSize);
        var cell     = grid.GetSafe(cx, cy);
        p.currentSlope = cell.slope;
        p.slopeVector  = new Vector2(cell.gradientX, cell.gradientY);
    }

    private static void ComputeSocialFleeAndSeparation(
        Animal self, AnimalState state, Vector2 pos, float range,
        List<Animal> nearby, SimulationSettings settings, ref PerceptionData p)
    {
        Vector2 socialSum     = Vector2.zero;
        Vector2 separationSum = Vector2.zero;
        Vector2 fleeSum       = Vector2.zero;
        int socialCnt = 0, predCnt = 0;

        float sepRadius = settings.separationRadius;
        float sepForce  = settings.separationForce;

        foreach (var other in nearby)
        {
            if (other == self || !other.IsAlive) continue;

            Vector2 otherPos = other.State.position;
            float dx = pos.x - otherPos.x, dz = pos.y - otherPos.y;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            if (dist > range || dist < 0.001f) continue;

            if (other.State.species == state.species)
            {
                float w = 1f - (dist / range);
                socialSum += (otherPos - pos) * w;
                socialCnt++;

                if (dist < sepRadius)
                {
                    float strength = (1f - dist / sepRadius) * sepForce;
                    separationSum += new Vector2(dx, dz).normalized * strength;
                }
            }
            else if (state.species == AnimalSpecies.Prey &&
                     other.State.species == AnimalSpecies.Predator)
            {
                float w = 1f - (dist / range);
                fleeSum += new Vector2(dx, dz).normalized * w;
                predCnt++;
            }
        }

        if (socialCnt > 0)
        {
            p.socialVector    = socialSum / socialCnt;
            p.socialCount     = socialCnt;
            p.separationVector = separationSum;
        }

        if (predCnt > 0)
        {
            p.fleeVector     = fleeSum.normalized;
            p.predatorNearby = true;
        }
    }

    private static void ComputeMate(
        Animal self, AnimalState state, Vector2 pos, float range,
        List<Animal> nearby, SimulationSettings settings, ref PerceptionData p)
    {
        float bestDist = float.MaxValue;
        foreach (var other in nearby)
        {
            if (other == self || !other.IsAlive) continue;
            if (other.State.species != state.species) continue;
            if (!other.State.CanMate(settings)) continue;
            float dist = Vector2.Distance(pos, other.State.position);
            if (dist < bestDist && dist <= range)
            {
                bestDist        = dist;
                p.mateFound     = true;
                p.mateCandidate = other;
                p.mateVector    = (other.State.position - pos).normalized;
            }
        }
    }

    private static void ComputeFoodPrey(
        Vector2 pos, float range, WorldGrid grid, RenderConfig cfg,
        PlantManager plantMgr, ref PerceptionData p)
    {
        plantMgr.GetFruitCellsInRadius(pos, range, _plantBuf);
        if (_plantBuf.Count == 0) return;

        float bestDist = float.MaxValue;
        Vector2 bestDir = Vector2.zero;

        foreach (var cell in _plantBuf)
        {
            float wx = cell.x * cfg.cellSize, wz = cell.y * cfg.cellSize;
            float dx = wx - pos.x, dz = wz - pos.y;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < bestDist) { bestDist = dist; bestDir = new Vector2(dx, dz); }
        }

        if (bestDist < range)
        {
            p.toFood      = bestDir.normalized * (1f - bestDist / range);
            p.foodFound   = true;
            p.foodDistance = bestDist;
        }
    }

    private static void ComputeFoodPredator(
        Animal self, AnimalState state, Vector2 pos, float range,
        List<Animal> nearby, ref PerceptionData p)
    {
        float bestDist = float.MaxValue;
        Vector2 bestDir = Vector2.zero;

        foreach (var other in nearby)
        {
            if (other == self || !other.IsAlive) continue;
            if (other.State.species != AnimalSpecies.Prey) continue;
            float dx = other.State.position.x - pos.x;
            float dz = other.State.position.y - pos.y;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist >= range || dist >= bestDist) continue;
            bestDist = dist; bestDir = new Vector2(dx, dz);
        }

        if (bestDist < range)
        {
            p.toFood      = bestDir.normalized * (1f - bestDist / range);
            p.foodFound   = true;
            p.foodDistance = bestDist;
        }
    }

    private static void ComputeWater(
        Vector2 pos, float range, WorldGrid grid, RenderConfig cfg,
        ref PerceptionData p)
    {
        int scanR = Mathf.CeilToInt(range / cfg.cellSize) + 1;
        int cx = Mathf.RoundToInt(pos.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / cfg.cellSize);

        float bestWaterDist = float.MaxValue, lowestHeight = float.MaxValue;
        Vector2 bestWaterDir = Vector2.zero, lowestDir = Vector2.zero;
        bool waterFound = false;

        for (int dx = -scanR; dx <= scanR; dx++)
            for (int dy = -scanR; dy <= scanR; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!grid.IsInside(nx, ny)) continue;

                float wx  = nx * cfg.cellSize, wz = ny * cfg.cellSize;
                float ddx = wx - pos.x,        ddz = wz - pos.y;
                float dist = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                if (dist > range) continue;

                var cell = grid.Get(nx, ny);
                if (cell.IsWater)
                {
                    if (dist < bestWaterDist)
                    {
                        bestWaterDist = dist;
                        bestWaterDir  = new Vector2(ddx, ddz);
                        waterFound    = true;
                    }
                }
                else if (cell.height < lowestHeight)
                {
                    lowestHeight = cell.height;
                    lowestDir    = new Vector2(ddx, ddz);
                }
            }

        if (waterFound)
        {
            p.toWater    = bestWaterDir.normalized * (1f - bestWaterDist / range);
            p.waterFound = true;
        }
        else if (lowestDir != Vector2.zero)
        {
            p.toWater    = lowestDir.normalized * 0.5f;
            p.waterFound = false;
        }
    }
}