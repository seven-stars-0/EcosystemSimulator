using UnityEngine;

public static class SteeringSystem
{
    public static Vector2 Compute(
        AnimalState state,
        PerceptionData p,
        SimulationSettings s,
        WorldGrid grid,
        RenderConfig cfg)
    {
        var g = state.genes;
        Vector2 brain = Vector2.zero;

        // 1. Calcolo repulsione bordi mappa
        Vector2 borderRepulsion = ComputeBorderRepulsion(state.position, grid, cfg);

        // 2. Calcolo dati della costa in tempo reale per vicinanza fisica
        GetShoreInfo(state.position, grid, cfg, out Vector2 shoreNormal, out float minWaterDist);
        // Distanza approssimativa dal bordo reale della cella d'acqua
        float distanceToShore = Mathf.Max(0f, minWaterDist - (cfg.cellSize * 0.5f));

        // ── CASO 1: EMERGENZA PREDATORE ───────────────────────────────────────
        if (p.predatorNearby)
        {
            brain += p.fleeVector * g.w_flee;
            brain += p.separationVector;
            brain += borderRepulsion;

            // Anche in fuga, se sta per schiantarsi in acqua, deviamo la forza a 90° lungo la riva
            brain = DeflectForceAlongShore(brain, shoreNormal, distanceToShore, state.velocity, cfg.cellSize);

            brain *= s.steeringForceScale;
            return ClampToMaxSpeed(brain, g.maxSpeed);
        }

        // ── CASO 2: STATO DI BEVUTA (Ancoraggio alla sponda) ──────────────────
        bool isDrinkingAtShore = (distanceToShore <= s.drinkingRange) && (state.thirst > state.needThreshold);

        if (isDrinkingAtShore)
        {
            // L'animale è a portata di sorso: AZZERIAMO il cervello. Si ferma immobile a bere.
            // Questo spegne sul nascere qualsiasi scatto o jittering.
            brain = Vector2.zero;
        }
        else
        {
            // ── CASO 3: NAVIGAZIONE STANDARD ──────────────────────────────────
            float hungerUrgency = (state.hunger > state.needThreshold && p.foodFound) ? state.hunger : 0f;
            float thirstUrgency = (state.thirst > state.needThreshold && p.waterFound) ? state.thirst : 0f;

            bool isInEmergency = hungerUrgency > state.needThreshold || thirstUrgency > state.needThreshold;

            if (isInEmergency)
            {
                if (thirstUrgency > hungerUrgency)
                    brain += p.toWater * g.w_water * (state.thirst * s.urgencyMax);
                else
                    brain += p.toFood * g.w_food * (state.hunger * s.urgencyMax);
            }
            else
            {
                if (state.CanMate(s) && p.mateFound)
                    brain += p.mateVector * s.mateSeekingBoost;
                else
                    brain += p.socialVector * g.w_social;
            }

            // Forze fisse ambientali
            brain += p.separationVector;
            brain += borderRepulsion;

            // RIMEDIO DISASTRO COSTIERO (w_slope):
            // Se l'animale è in emergenza, OPPURE si trova molto vicino alla costa (entro 2 celle),
            // disattiviamo completamente l'influenza della pendenza (w_slope).
            // Questo impedisce che l'altezza negativa dell'acqua crei una trappola gravitazionale evolutiva.
            if (!isInEmergency && distanceToShore > cfg.cellSize * 2f && p.slopeVector.sqrMagnitude > 0.001f)
            {
                brain += p.slopeVector.normalized * g.w_slope;
            }

            // Esplorazione (Wander)
            float wanderWeight = isInEmergency ? 0f : 1.0f;
            brain += p.wanderVector * Mathf.Max(0f, g.w_curiosity) * wanderWeight;
        }

        // Se l'animale si sta muovendo (non sta bevendo) ed è vicino all'acqua, 
        // intercettiamo la forza finale. Se punta verso l'acqua, la ruotiamo di 90° lungo la costa.
        if (!isDrinkingAtShore)
        {
            brain = DeflectForceAlongShore(brain, shoreNormal, distanceToShore, state.velocity, cfg.cellSize);
        }

        // Scaling finale e Clamping alla velocità massima dell'animale
        brain *= s.steeringForceScale;
        brain = ClampToMaxSpeed(brain, g.maxSpeed);

        // Lo sliding rimane attivo alla fine come "paracadute fisico" passivo
        brain = ApplyWaterSliding(brain, state.position, grid, cfg);

        return brain;
    }

    // Calcola se siamo vicini all'acqua e ricava il vettore Normale della costa (diretto verso la terra)
    private static void GetShoreInfo(Vector2 pos, WorldGrid grid, RenderConfig cfg, out Vector2 shoreNormal, out float minDistanceToWater)
    {
        shoreNormal = Vector2.zero;
        minDistanceToWater = float.MaxValue;

        int cx = Mathf.RoundToInt(pos.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / cfg.cellSize);

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!grid.IsInside(nx, ny)) continue;
                if (!grid.Get(nx, ny).IsWater) continue;

                float wx = nx * cfg.cellSize;
                float wz = ny * cfg.cellSize;
                float ddx = pos.x - wx, ddz = pos.y - wz;
                float dist = Mathf.Sqrt(ddx * ddx + ddz * ddz);

                if (dist < minDistanceToWater)
                    minDistanceToWater = dist;

                if (dist > 0.001f)
                {
                    // Accumula i vettori di allontanamento dall'acqua
                    shoreNormal += new Vector2(ddx / dist, ddz / dist) * (cfg.cellSize / dist);
                }
            }

        if (shoreNormal.sqrMagnitude > 0.001f)
            shoreNormal = shoreNormal.normalized;
    }

    // Applica la rotazione di 90 gradi se la forza punta verso lo specchio d'acqua
    private static Vector2 DeflectForceAlongShore(Vector2 force, Vector2 shoreNormal, float distanceToShore, Vector2 currentVelocity, float cellSize)
    {
        // Attiviamo il costeggiamento solo se siamo molto vicini all'acqua (es. entro 1.5 celle dal bordo)
        if (distanceToShore > cellSize * 1.5f || shoreNormal.sqrMagnitude < 0.001f || force.sqrMagnitude < 0.001f)
            return force;

        // Se il prodotto scalare tra la forza e la normale è negativo, significa che l'animale sta tentando 
        // di camminare DENTRO l'acqua (vettori speculari)
        if (Vector2.Dot(force, shoreNormal) < 0f)
        {
            // Calcoliamo le due possibili tangenti a 90 gradi (Sinistra e Destra rispetto alla costa)
            Vector2 tangentLeft = new Vector2(-shoreNormal.y, shoreNormal.x);
            Vector2 tangentRight = new Vector2(shoreNormal.y, -shoreNormal.x);

            // Scegliamo la tangente che asseconda il movimento corrente dell'animale (Inerzia), 
            // se è fermo assecondiamo la tendenza della forza stessa
            Vector2 referenceDir = currentVelocity.sqrMagnitude > 0.01f ? currentVelocity : force;

            Vector2 chosenTangent = Vector2.Dot(referenceDir, tangentLeft) > Vector2.Dot(referenceDir, tangentRight)
                ? tangentLeft
                : tangentRight;

            // Ruotiamo la forza a 90° tenendo la stessa identica magnitudo (velocità di scorrimento)
            return chosenTangent * force.magnitude;
        }

        return force;
    }

    private static Vector2 ClampToMaxSpeed(Vector2 force, float maxSpeed)
    {
        if (force.sqrMagnitude > maxSpeed * maxSpeed)
            return force.normalized * maxSpeed;
        return force;
    }

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
            velocity -= dot * shoreNormal;
        }

        return velocity;
    }

    private static Vector2 ComputeBorderRepulsion(Vector2 pos, WorldGrid grid, RenderConfig cfg)
    {
        float maxW = (grid.size - 1) * cfg.cellSize;
        float borderRadius = 4f * cfg.cellSize;
        const float borderForce = 15f;
        Vector2 rep = Vector2.zero;

        float dL = pos.x; if (dL < borderRadius) rep.x += borderForce * (1f - dL / borderRadius);
        float dR = maxW - pos.x; if (dR < borderRadius) rep.x -= borderForce * (1f - dR / borderRadius);
        float dB = pos.y; if (dB < borderRadius) rep.y += borderForce * (1f - dB / borderRadius);
        float dT = maxW - pos.y; if (dT < borderRadius) rep.y -= borderForce * (1f - dT / borderRadius);

        return rep;
    }
}