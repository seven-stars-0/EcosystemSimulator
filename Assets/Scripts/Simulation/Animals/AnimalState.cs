using UnityEngine;

public enum AnimalSpecies { Prey, Predator }

public class AnimalState
{
    public int id;
    public AnimalSpecies species;
    public GeneticProfile genes;

    public float energy = 1.0f;
    public float hunger = 0.0f;
    public float thirst = 0.0f;
    public float needThreshold = 0.65f;
    public float matingRange = 5f;

    public Vector2 position;
    public Vector2 velocity;
    public Vector2 wanderDir = Vector2.right;

    public float reproductionCooldown = 0f;
    public float attackCooldown = 0f;
    public int offspringCount = 0;
    public float age = 0f;

    public const float MATURITY_AGE = 25f;

    // ── Tracciamento parentela (anti-inbreeding) ──────────────────────────────
    // -1 = nessun genitore tracciato (prima generazione o animali editor).
    // Impostati da ReproductionSystem.CreateOffspring().

    public int parentAId = -1;
    public int parentBId = -1;

    // ── Proprietà ─────────────────────────────────────────────────────────────

    public bool IsAlive
        => energy > 0f && hunger < 1f && thirst < 1f;

    public bool CanMate(SimulationSettings s)
        => energy >= s.reproductionThreshold
        && reproductionCooldown <= 0f
        && age >= MATURITY_AGE;
    // VEDI SE AGGIUNGERE hunger E thirst COME REQUISITI (ma non dovrebbe servire a causa dello SteeringSystem)

    public float Speed => velocity.magnitude;

    // Serve per sapere se l'animale è un cucciolo (serve nel metabolismo)
    public bool IsCub => age < MATURITY_AGE;
}
