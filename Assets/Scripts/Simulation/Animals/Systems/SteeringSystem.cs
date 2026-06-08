using UnityEngine;

public static class SteeringSystem
{
    /// <summary>
    /// Soglia minima di fame/sete sotto la quale il vettore cibo/acqua
    /// non contribuisce allo steering. Evita che animali sazi vengano
    /// costantemente attratti da risorse che non gli servono.
    /// </summary>
    private const float NeedThreshold = 0.15f;

    public static Vector2 Compute(
        AnimalState        state,
        PerceptionData     p,
        SimulationSettings s,
        WorldGrid          grid,
        RenderConfig       cfg)
    {
        var g = state.genes;

        Vector2 accel = Vector2.zero;

        // ── Cibo ──────────────────────────────────────────────────────────────
        // Contribuisce solo se l'animale ha davvero fame.
        // urgency: 0 a hunger=0, rampa fino a urgencyMax a hunger=1.
        if (state.hunger > NeedThreshold && p.foodFound)
        {
            float hungerUrgency = state.hunger * s.urgencyMax;
            accel += p.toFood * g.w_food * hungerUrgency;
        }

        // ── Acqua ─────────────────────────────────────────────────────────────
        // Contribuisce solo se l'animale ha davvero sete.
        // Stesso schema: 0 a thirst=0, ramp fino a urgencyMax a thirst=1.
        if (state.thirst > NeedThreshold)
        {
            float thirstUrgency = state.thirst * s.urgencyMax;
            accel += p.toWater * g.w_water * thirstUrgency;
        }

        // ── Sociale / Seek mate ───────────────────────────────────────────────
        if (state.CanMate(s) && p.mateFound)
            accel += p.mateVector * s.mateSeekingBoost;
        else
            accel += p.socialVector * g.w_social;

        // Separazione: sempre attiva
        accel += p.separationVector;

        // ── Fuga predatori ────────────────────────────────────────────────────
        if (p.predatorNearby)
            accel += p.fleeVector * g.w_flee;

        // ── Terreno ───────────────────────────────────────────────────────────
        accel += p.slopeVector * g.w_slope;

        // ── Wander (esplorazione casuale) ─────────────────────────────────────
        // Questo è il motore principale dell'esplorazione quando gli altri
        // stimoli sono deboli (niente cibo vicino, non affamato, ecc.)
        accel += p.wanderVector * Mathf.Max(0f, g.w_curiosity);

        // ── Repulsioni fisiche ────────────────────────────────────────────────
        accel += ComputeBorderRepulsion(state.position, grid, cfg);
        accel += ComputeWaterRepulsion(state.position, grid, cfg);

        // ── Scala e clamp ─────────────────────────────────────────────────────
        accel *= s.steeringForceScale;

        float maxSpeed = g.EffectiveMaxSpeed;
        if (accel.sqrMagnitude > maxSpeed * maxSpeed)
            accel = accel.normalized * maxSpeed;

        return accel;
    }

    // ── Border repulsion ──────────────────────────────────────────────────────

    private static Vector2 ComputeBorderRepulsion(Vector2 pos, WorldGrid grid, RenderConfig cfg)
    {
        float maxW         = (grid.size - 1) * cfg.cellSize;
        float borderRadius = 4f * cfg.cellSize;
        const float borderForce = 8f;
        Vector2 rep = Vector2.zero;

        float dL = pos.x;        if (dL < borderRadius) rep.x += borderForce * (1f - dL / borderRadius);
        float dR = maxW - pos.x; if (dR < borderRadius) rep.x -= borderForce * (1f - dR / borderRadius);
        float dB = pos.y;        if (dB < borderRadius) rep.y += borderForce * (1f - dB / borderRadius);
        float dT = maxW - pos.y; if (dT < borderRadius) rep.y -= borderForce * (1f - dT / borderRadius);

        return rep;
    }

    // ── Water repulsion ───────────────────────────────────────────────────────

    private static Vector2 ComputeWaterRepulsion(Vector2 pos, WorldGrid grid, RenderConfig cfg)
    {
        const float waterForce  = 12f;
        const float waterRadius = 1.5f;

        int cx = Mathf.RoundToInt(pos.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / cfg.cellSize);

        Vector2 rep = Vector2.zero;

        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = cx + dx, ny = cy + dy;
                if (!grid.IsInside(nx, ny)) continue;
                if (!grid.Get(nx, ny).IsWater) continue;

                float wx   = nx * cfg.cellSize;
                float wz   = ny * cfg.cellSize;
                float ddx  = pos.x - wx, ddz = pos.y - wz;
                float dist = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                float cellDist = dist / cfg.cellSize;

                if (cellDist < waterRadius && dist > 0.001f)
                    rep += new Vector2(ddx, ddz).normalized * (waterForce * (1f - cellDist / waterRadius));
            }

        return rep;
    }
}