using UnityEngine;

public enum AnimalSpecies { Prey, Predator }

// ============================================================================
//  AnimalState  -  Stato a energia (demografia per-agente: energia/fame/morte).
// ============================================================================

public class AnimalState
{
    public int id;
    public AnimalSpecies species;
    public GeneticProfile genes;

    public float energy = 0.85f;   // valuta demografica: morte a 0, riproduzione sopra soglia
    public float hunger = 0.0f;    // [0,1] motivazione: guida foraging/caccia, accelera il drain

    public Vector2 position;
    public Vector2 velocity;
    public Vector2 wanderDir = Vector2.right;

    public float reproductionCooldown = 0f;
    public float handlingCooldown = 0f;   // predatore: "digestione" post-attacco (handling time)

    public bool IsAlive => energy > 0f && hunger < 1f;

    public bool CanReproduce(SimulationSettings s)
    {
        float threshold = species == AnimalSpecies.Prey ? s.preyReproThreshold : s.predatorReproThreshold;
        return energy >= threshold && reproductionCooldown <= 0f;
    }
}
