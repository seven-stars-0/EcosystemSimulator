using System.Collections.Generic;
using UnityEngine;

public static class EcologySystem
{
    private static readonly List<Animal> _preyBuf = new(32);
    private static readonly List<Animal> _refugeBuf = new(16);
    private static readonly List<Animal> _predBuf = new(16);

    private const float REFUGE_RADIUS = 3.5f;
    private const float REFUGE_FULL   = 3f;
    private const float REFUGE_FLOOR  = 0.45f;
    private const float SATIATION     = 0.80f;
    private const float INTERFERENCE_RADIUS = 5f;

    // Nodifica i valori dello stato dell'animale che dipendono dallo scorrere del tempo
    public static void Metabolize(AnimalState a, SimulationSettings s, float dt)
    {
        float drain = (a.species == AnimalSpecies.Prey) ? s.preyMetabolicDrain : s.predatorMetabolicDrain;
        drain *= 1f + a.hunger;
        a.energy = Mathf.Max(0f, a.energy - drain * dt);
        a.hunger = Mathf.Clamp01(a.hunger + s.hungerRate * dt);

        if (a.reproductionCooldown > 0f) a.reproductionCooldown -= dt;
        if (a.handlingCooldown > 0f)     a.handlingCooldown     -= dt;
    }

    // SOLO PREDE. Se c'è una pianta con frutto, la mangia e modifica i valori di energia e fame
    public static void Graze(AnimalState a, PlantManager plants, SimulationSettings s,
                             WorldGrid grid, RenderConfig cfg)
    {
        int cx = Mathf.RoundToInt(a.position.x / cfg.cellSize);
        int cy = Mathf.RoundToInt(a.position.y / cfg.cellSize);
        if (!plants.TryEat(cx, cy, s)) return;
        a.energy = Mathf.Min(s.energyMax, a.energy + s.preyEnergyPerPlant);
        a.hunger = Mathf.Max(0f, a.hunger - 0.6f);
    }

    // SOLO PREDATORI
    public static Animal Hunt(Animal predator, AnimalState a, SpatialGrid<Animal> spatial, SimulationSettings s)
    {
        // Se il predatore deve ancora digerire, o non ha livelli di energia e fame tali da giustificare
        // la caccia, allora non lo fa. Questo serve per evitare che i predatori si mettano a sterminare le prede
        // a prescindere da quanta fame abbiano
        if (a.handlingCooldown > 0f) return null;
        if (a.energy >= s.energyMax * SATIATION) return null;
        if (a.hunger < 0.05f) return null;

        // Rifacciamo una query, ma usando attackRange come range visivo
        spatial.Query(a.position, s.attackRange, _preyBuf);

        float interference = PredatorInterference(predator, spatial, s.predatorInterference);

        foreach (var prey in _preyBuf)
        {
            // Evita se stesso, morti, altri predatori, e prede al di fuori dell'attackRange
            if (prey == predator || !prey.IsAlive) continue;
            if (prey.State.species != AnimalSpecies.Prey) continue;
            if (Vector2.Distance(a.position, prey.State.position) > s.attackRange) continue;

            float refuge = RefugeFactor(prey, spatial);
            // Se riesce ad ucciderlo, il predatore si sfama e comincia la digestione
            if (Random.value < s.killChance * refuge * interference)
            {
                a.energy = Mathf.Min(s.energyMax, a.energy + s.predatorEnergyPerPrey);
                a.hunger = Mathf.Max(0f, a.hunger - 0.6f);
                a.handlingCooldown = s.handlingTime;
                return prey;
            }

            // Se fallisce l'attacco, attende un po' prima di riprovare, altrimenti spammerebbe Hunt e l'uccisione diventerebbe certa
            a.handlingCooldown = 0.5f;
            return null;
        }
        return null;
    }

    // Una preda con pochi simili attorno è più difficile da catturare
    // Da' alle prede rare/disperse una via di scampo emergente
    private static float RefugeFactor(Animal victim, SpatialGrid<Animal> spatial)
    {
        spatial.Query(victim.State.position, REFUGE_RADIUS, _refugeBuf);
        int preyNear = 0;
        foreach (var o in _refugeBuf)
            if (o != null && o.IsAlive && o.State.species == AnimalSpecies.Prey)
                preyNear++;
        preyNear = Mathf.Max(0, preyNear - 1);
        return Mathf.Lerp(REFUGE_FLOOR, 1f, Mathf.Clamp01(preyNear / REFUGE_FULL));
    }

    // Più predatori vicini rendono caccia meno efficace
    // Forza = k (settings.predatorInterference). Se k=0, nessuna interferenza
    private static float PredatorInterference(Animal hunter, SpatialGrid<Animal> spatial, float k)
    {
        if (k <= 0f) return 1f;
        spatial.Query(hunter.State.position, INTERFERENCE_RADIUS, _predBuf);
        int predNear = 0;
        foreach (var o in _predBuf)
            if (o != null && o != hunter && o.IsAlive && o.State.species == AnimalSpecies.Predator)
                predNear++;
        return 1f / (1f + k * predNear);
    }

    // Mortalita' dei PREDATORI dovuta alla scarsità di prede (Leslie-Gower)
    // Dato il rapporto globale R = prede/predatori, se R < ComfortRatio la
    // probabilita' di morte cresce linearmente fino al massimo a R = 0
    // Sopra la soglia restituisce false (nessuna morte extra). Estrazione casuale
    // per-individuo -> morti desincronizzate (niente spalle nel grafico, prima succedeva), e il
    // termine e' auto-limitante: meno predatori -> R piu' alto -> si spegne.
    public static bool ScarcityDeath(int preyCount, int predatorCount, SimulationSettings s, float dt)
    {
        if (s.predatorScarcityMortality <= 0f || predatorCount <= 0) return false;
        float ratio    = preyCount / (float)predatorCount; // prede per predatore
        float severity = Mathf.Clamp01(1f - ratio / Mathf.Max(1e-3f, s.predatorComfortRatio));
        if (severity <= 0f) return false;
        return Random.value < s.predatorScarcityMortality * severity * dt;
    }

    public static bool TryReproduce(AnimalState a, SimulationSettings s)
    {
        if (!a.CanReproduce(s)) return false;
        float cost     = (a.species == AnimalSpecies.Prey) ? s.preyReproCost     : s.predatorReproCost;
        float cooldown = (a.species == AnimalSpecies.Prey) ? s.preyReproCooldown : s.predatorReproCooldown;
        a.energy = Mathf.Max(0f, a.energy - cost);
        a.reproductionCooldown = cooldown;
        return true;
    }
}
