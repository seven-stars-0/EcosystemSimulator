using UnityEngine;

public static class SteeringSystem
{
    private const float NeedThreshold = 0.15f;

    public static Vector2 Compute(
        AnimalState state,
        PerceptionData p,
        SimulationSettings s,
        WorldGrid grid,
        RenderConfig cfg)
    {
        var g = state.genes;
        Vector2 brain = Vector2.zero;

        // ── FASE 1: Priorità Biologica (Previene l'annullamento dei vettori) ──
        if (p.predatorNearby)
        {
            // Se c'è un predatore, la priorità ASSOLUTA è scappare.
            // Ignoriamo cibo e partner per evitare che i vettori si annullino.
            brain += p.fleeVector * g.w_flee;

            // Manteniamo l'acqua solo se la sete è critica, ma fortemente ridotta
            if (state.thirst > 0.5f)
                brain += p.toWater * g.w_water * (state.thirst * s.urgencyMax * 0.3f);
        }
        else
        {
            // Comportamento di Routine (Nessun pericolo di vita imminente)

            // Cibo: attivo solo sopra soglia
            if (state.hunger > NeedThreshold && p.foodFound)
                brain += p.toFood * g.w_food * (state.hunger * s.urgencyMax);

            // Acqua: attivo solo sopra soglia
            if (state.thirst > NeedThreshold && p.waterFound)
                brain += p.toWater * g.w_water * (state.thirst * s.urgencyMax);

            // Sociale / Accoppiamento
            if (state.CanMate(s) && p.mateFound)
                brain += p.mateVector * s.mateSeekingBoost;
            else
                brain += p.socialVector * g.w_social;
        }

        // Separazione e Terreno (Sempre attivi nel cervello)
        brain += p.separationVector;
        brain += p.slopeVector * g.w_slope;

        // Wander (Motore di ricerca)
        brain += p.wanderVector * Mathf.Max(0f, g.w_curiosity);

        // Scala globale e CLAMP DEL CERVELLO alle capacità fisiche dell'animale
        brain *= s.steeringForceScale;
        float maxSpeed = g.EffectiveMaxSpeed;
        if (brain.sqrMagnitude > maxSpeed * maxSpeed)
            brain = brain.normalized * maxSpeed;

        // ── FASE 2: Correzioni Ambientali (Post-Clamp) ──────────────────────

        // 1. Sliding dell'acqua (Evita il blocco sulle rive)
        brain = ApplyWaterSliding(brain, state.position, grid, cfg);

        // 2. Repulsione dei Bordi (Claude l'aveva rimossa! Rimessa qui come forza coercitiva)
        brain += ComputeBorderRepulsion(state.position, grid, cfg);

        return brain;
    }

    // ── Water sliding ─────────────────────────────────────────────────────────
    private static Vector2 ApplyWaterSliding(Vector2 velocity, Vector2 pos, WorldGrid grid, RenderConfig cfg)
    {
        if (velocity.sqrMagnitude < 0.001f) return velocity;

        int cx = Mathf.RoundToInt(pos.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / cfg.cellSize);

        Vector2 shoreNormal = Vector2.zero;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = cx + dx, ny = cy + dy;
                if (!grid.IsInside(nx, ny)) continue;
                if (!grid.Get(nx, ny).IsWater) continue;

                float wx = nx * cfg.cellSize;
                float wz = ny * cfg.cellSize;
                float ddx = pos.x - wx, ddz = pos.y - wz;
                float dist = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                if (dist < 0.001f) continue;

                shoreNormal += new Vector2(ddx / dist, ddz / dist) * (cfg.cellSize / dist);
            }

        if (shoreNormal.sqrMagnitude < 0.001f) return velocity;
        shoreNormal = shoreNormal.normalized;

        float dot = Vector2.Dot(velocity, shoreNormal);
        if (dot < 0f)
        {
            // Rimuove la componente che punta verso l'acqua, lasciando intatta la tangente
            velocity -= dot * shoreNormal;
        }

        return velocity;
    }

    // ── Border repulsion ──────────────────────────────────────────────────────
    private static Vector2 ComputeBorderRepulsion(Vector2 pos, WorldGrid grid, RenderConfig cfg)
    {
        float maxW = (grid.size - 1) * cfg.cellSize;
        float borderRadius = 4f * cfg.cellSize;
        const float borderForce = 15f; // Alzata leggermente per contrastare spinte forti
        Vector2 rep = Vector2.zero;

        float dL = pos.x; if (dL < borderRadius) rep.x += borderForce * (1f - dL / borderRadius);
        float dR = maxW - pos.x; if (dR < borderRadius) rep.x -= borderForce * (1f - dR / borderRadius);
        float dB = pos.y; if (dB < borderRadius) rep.y += borderForce * (1f - dB / borderRadius);
        float dT = maxW - pos.y; if (dT < borderRadius) rep.y -= borderForce * (1f - dT / borderRadius);

        return rep;
    }
}