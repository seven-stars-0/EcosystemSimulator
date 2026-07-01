using UnityEngine;

public static class SteeringSystem
{
    // Pesi di movimento (costanti di tuning del comportamento)
    private const float WANDER        = 1.0f;
    private const float PREY_ALIGN    = 0.9f;
    private const float PREY_FOOD     = 1.8f;
    private const float PREY_FLEE     = 3.0f;
    private const float PRED_WANDER   = 1.4f;
    private const float PRED_CHASE    = 2.4f;
    private const float SEPARATION    = 1.3f;
    private const float WATER_AVOID   = 3.0f;

    // Mappatura urgenza -> velocita': |desired| >= CRUISE_REF -> velocita' piena;
    // sotto, crociera proporzionale (mai sotto MIN_SPEED_FRAC, per non congelarsi).
    private const float CRUISE_REF     = 3.0f;
    private const float MIN_SPEED_FRAC = 0.3f;

    public static Vector2 Compute(
        AnimalState state, in PerceptionData p, SimulationSettings s,
        WorldGrid grid, RenderConfig cfg)
    {
        // Prendiamo i geni che scaleranno le direzioni
        var g = state.genes;

        Vector2 border = ComputeBorderRepulsion(state.position, grid, cfg);
        GetShoreInfo(state.position, grid, cfg, out Vector2 awayFromWater, out float minWaterDist);

        Vector2 desired;

        // Caso prede
        if (state.species == AnimalSpecies.Prey)
        {
            // Se c'è un predatore, la preda corre in direzione opposta
            // tenendo anche conto della forza di separazione tra conspecifici (altrimenti si sovrappongono)
            // e della wanderDir
            if (p.predatorNearby)
            {
                desired = p.fleeDir * PREY_FLEE
                        + p.separation * SEPARATION
                        + p.wanderDir * (WANDER * 0.3f);
            }
            else
            {
                // Peso del vettore toFood, viene scalato in base alla fame
                float forage = Mathf.Clamp01(0.35f + state.hunger);
                float social = g.social;

                desired = p.wanderDir * WANDER
                        + p.cohesionDir * social
                        + p.alignmentDir * (Mathf.Max(0f, social) * PREY_ALIGN)
                        + p.separation * SEPARATION
                        + (p.foodFound ? p.toFood * (PREY_FOOD * forage) : Vector2.zero); // forage viene usati solo se c'è cibo
            }
        }
        // Caso predatori
        else
        {
            desired = p.wanderDir * PRED_WANDER
                    + p.cohesionDir * g.social
                    + p.separation * SEPARATION
                    + (p.foodFound ? p.toFood * PRED_CHASE : Vector2.zero); // I predatori inseguono sempre (caccia NON scalata dalla fame)
        }

        // Sommiamo a desired la repulsione dei bordi
        desired += border;

        // Gli animali evitano l'acqua. Questo serve per evitare un bug che mi ha fatto impazzire, dove gli animali si ammucchiavano
        // lungo le coste, godendosi il tramonto con il proprio partner. E mannaggia se era piacevole, visto che preferivano morire di fame
        // pur di non allontanarsi dalla costa
        float avoidRange = cfg.cellSize * 2.5f;
        if (minWaterDist < avoidRange && awayFromWater.sqrMagnitude > 1e-6f)
            desired += awayFromWater * (WATER_AVOID * (1f - minWaterDist / avoidRange));

        Vector2 dir = desired.sqrMagnitude > 1e-6f ? desired.normalized : p.wanderDir;

        // La MAGNITUDINE di 'desired' e' l'urgenza: forze deboli (solo wander) ->
        // crociera lenta; forze forti (fuga/caccia/acqua) -> sprint fino a maxSpeed.
        // Cosi' l'animale sceglie la velocita' e non corre sempre al massimo.
        float urgency = Mathf.Clamp01(desired.magnitude / CRUISE_REF);
        float speed   = g.maxSpeed * Mathf.Max(MIN_SPEED_FRAC, urgency);
        Vector2 velCmd = dir * speed;

        velCmd = ProjectAwayFromWater(velCmd, awayFromWater, minWaterDist, cfg.cellSize);
        return velCmd;
    }

    // Calcola nel raggio di 2 eventuali celle ad altezza negativa (acqua), e restituisce la distanza minima dall'acqua
    // e il vettore direzione opposta
    private static void GetShoreInfo(
        Vector2 pos, WorldGrid grid, RenderConfig cfg,
        out Vector2 awayFromWater, out float minDistanceToWater)
    {
        awayFromWater = Vector2.zero;
        minDistanceToWater = float.MaxValue;

        int cx = Mathf.RoundToInt(pos.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / cfg.cellSize);

        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!grid.IsInside(nx, ny)) continue;
                if (!grid.Get(nx, ny).IsWater) continue;

                float wx = nx * cfg.cellSize, wz = ny * cfg.cellSize;
                float ddx = pos.x - wx, ddz = pos.y - wz;
                float dist = Mathf.Sqrt(ddx * ddx + ddz * ddz);

                if (dist < minDistanceToWater) minDistanceToWater = dist;
                if (dist > 0.01f)
                    awayFromWater += new Vector2(ddx / dist, ddz / dist) / dist;
            }

        if (awayFromWater.sqrMagnitude > 1e-6f) awayFromWater = awayFromWater.normalized;
    }

    // Prende quanto calcolato in GetShoreInfo e restituisce la forza di repulsione, inversamente proporzionale
    // alla distanza dall'acqua
    private static Vector2 ProjectAwayFromWater(
        Vector2 force, Vector2 awayFromWater, float distanceToShore, float cellSize)
    {
        if (distanceToShore > cellSize * 1.2f
            || awayFromWater.sqrMagnitude < 1e-6f
            || force.sqrMagnitude < 1e-6f)
            return force;

        float dot = Vector2.Dot(force, awayFromWater);
        if (dot < 0f) force -= dot * awayFromWater;
        return force;
    }

    // Semplicissimo, calcola se le celle adiacenti sono bordo e mette in rep le direzioni opposte
    // per evitare che gli animali cadano giù
    // In Animal.ApplySteering viene completamente impedito che gli animali fuoriescano
    // Infatti potrebbe succedere che un predatore insegue una preda verso i bordi, quindi il vettore fuga ha maggior peso
    // rispetto a rep. Fidatevi, lo so molto bene
    private static Vector2 ComputeBorderRepulsion(Vector2 pos, WorldGrid grid, RenderConfig cfg)
    {
        float maxW = (grid.size - 1) * cfg.cellSize;
        float borderRadius = 4f * cfg.cellSize;
        const float borderForce = 3f;
        Vector2 rep = Vector2.zero;

        float dL = pos.x;        if (dL < borderRadius) rep.x += borderForce * (1f - dL / borderRadius);
        float dR = maxW - pos.x; if (dR < borderRadius) rep.x -= borderForce * (1f - dR / borderRadius);
        float dB = pos.y;        if (dB < borderRadius) rep.y += borderForce * (1f - dB / borderRadius);
        float dT = maxW - pos.y; if (dT < borderRadius) rep.y -= borderForce * (1f - dT / borderRadius);
        return rep;
    }
}
