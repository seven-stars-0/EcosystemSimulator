using System.Collections.Generic;
using UnityEngine;

// Tutti i vettori generati sono normalizzati
public static class PerceptionSystem
{
    private static readonly List<Animal> _buf = new(64);
    private static readonly List<Vector2Int> _plantBuf = new(32);

    public static PerceptionData Compute(
        Animal animal, AnimalState state,
        WorldGrid grid, RenderConfig cfg,
        SpatialGrid<Animal> animalGrid, PlantManager plantMgr,
        SimulationSettings s, float dt)
    {
        var p = new PerceptionData();
        float range = state.genes.visionRange;
        Vector2 pos = state.position;

        p.wanderDir = UpdateWander(state.wanderDir, dt);
        state.wanderDir = p.wanderDir;

        animalGrid.Query(pos, range, _buf);
        ComputeNeighbors(animal, state, pos, range, _buf, s, ref p);

        if (state.species == AnimalSpecies.Prey)
            ComputeFoodPrey(pos, range, cfg, plantMgr, ref p);
        // Il caso dei predatori è gestito in ComputeNeighbors

        return p;
    }

    // Wander DECORRELATO: moto quasi rettilineo con occasionali cambi di direzione
    //
    // Il vecchio random-walk continuo sull'angolo, inseguito dal filtro di velocita'
    // in ritardo, produceva traiettorie circolari. Ora ogni ~3s si sceglie una nuova
    // direzione a caso, e nel mezzo si perturba solo lievemente -> niente cerchi.
    private static Vector2 UpdateWander(Vector2 current, float dt)
    {
        if (current.sqrMagnitude < 1e-6f) current = Vector2.right;

        if (Random.value < 0.3f * dt)   // in media ~ ogni 3s di tempo simulato
        {
            float a = Random.value * 2f * Mathf.PI;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        float angle = Mathf.Atan2(current.y, current.x)
                    + Random.Range(-25f, 25f) * Mathf.Deg2Rad * dt;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private static void ComputeNeighbors(
        Animal self, AnimalState state, Vector2 pos, float range,
        List<Animal> nearby, SimulationSettings s, ref PerceptionData p)
    {
        Vector2 center = Vector2.zero;     // somma posizioni conspecifici (coesione)
        Vector2 heading = Vector2.zero;    // somma velocita' conspecifici (allineamento)
        Vector2 separation = Vector2.zero; // spinte di allontanamento
        Vector2 flee = Vector2.zero;       // spinte di fuga (solo prede)
        int count = 0;

        float bestPrey = float.MaxValue; Vector2 preyDir = Vector2.zero; bool preyFound = false;
        const float sepR = 2.5f;  // raggio personal space (costante)

        foreach (var other in nearby)
        {
            if (other == self || !other.IsAlive) continue;
            var os = other.State;

            float dx = pos.x - os.position.x, dz = pos.y - os.position.y;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist > range || dist < 0.0001f) continue; // Escludiamo gli animali al di fuori del range

            // Caso conspecifici
            if (os.species == state.species)
            {
                center  += os.position;
                heading += os.velocity;
                count++;
                // Se sono troppo vicini, aggiorniamo il vettore separazione per andare in direzione opposta
                if (dist < sepR)
                    separation += new Vector2(dx, dz).normalized * (1f - dist / sepR);
            }
            else if (state.species == AnimalSpecies.Prey)   // Se l'altro è un predatore
            {
                // Fuga piu' forte quanto piu' vicino il predatore
                flee += new Vector2(dx, dz).normalized * (1f - dist / range);
                p.predatorNearby = true;
            }
            else // Se l'altro è una preda
            {
                // Qui calcoliamo il "cibo" più vicino
                if (dist < bestPrey) { bestPrey = dist; preyDir = os.position - pos; preyFound = true; }
            }
        }

        p.neighborCount = count;
        p.separation = separation;

        // Centro di massa dei conspecifici
        if (count > 0)
        {
            Vector2 toCenter = (center / count) - pos;
            if (toCenter.sqrMagnitude > 1e-6f) p.cohesionDir = toCenter.normalized;
            if (heading.sqrMagnitude > 1e-6f)  p.alignmentDir = heading.normalized;
        }

        if (p.predatorNearby && flee.sqrMagnitude > 1e-6f) p.fleeDir = flee.normalized;

        // Caso predatori, se hanno trovato preda impostano il vettore direzione verso cibo
        if (preyFound && preyDir.sqrMagnitude > 1e-6f)
        {
            p.toFood = preyDir.normalized;
            p.foodFound = true;
        }
    }

    // Indirizza la preda alla pianta con frutto più vicina
    // Non lo spiego oltre perché è simile a molti altri metodi
    private static void ComputeFoodPrey(
        Vector2 pos, float range, RenderConfig cfg, PlantManager plantMgr, ref PerceptionData p)
    {
        plantMgr.GetFruitCellsInRadius(pos, range, _plantBuf);
        if (_plantBuf.Count == 0) return;

        float bestDist = float.MaxValue;
        Vector2 bestDir = Vector2.zero;

        foreach (var cell in _plantBuf)
        {
            float wx = cell.x * cfg.cellSize, wz = cell.y * cfg.cellSize;
            float dx = wx - pos.x, dz = wz - pos.y;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < bestDist) { bestDist = d; bestDir = new Vector2(dx, dz); }
        }

        if (bestDir.sqrMagnitude > 1e-6f)
        {
            p.toFood = bestDir.normalized;
            p.foodFound = true;
        }
    }
}
