using UnityEngine;
 
public enum AnimalSpecies { Prey, Predator }
 
public class AnimalState
{
    public int           id;
    public AnimalSpecies species;
    public GeneticProfile genes;
 
    public float energy = 1.0f;
    public float hunger = 0.0f;
    public float thirst = 0.0f;
 
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 wanderDir = Vector2.right;
 
    public float reproductionCooldown = 0f;
    public float attackCooldown       = 0f;   // solo predatori
    public int   offspringCount       = 0;
    public float age                  = 0f;
 
    public bool IsAlive
        => energy > 0f && hunger < 1f && thirst < 1f;
 
    public bool CanMate(SimulationSettings s)
        => energy >= genes.reproductionThreshold
        && reproductionCooldown <= 0f;
 
    public float EnergyNormalized => energy / genes.EnergyMax;
    public float Speed            => velocity.magnitude;
}
 