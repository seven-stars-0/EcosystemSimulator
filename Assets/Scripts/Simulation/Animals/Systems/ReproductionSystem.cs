using UnityEngine;
 
public static class ReproductionSystem
{
    public static GeneticProfile TryMate(AnimalState a, AnimalState b, SimulationSettings s)
    {
        if (!a.CanMate(s) || !b.CanMate(s)) return null;
        if (a.species != b.species)         return null;
 
        float dist = Vector2.Distance(a.position, b.position);
        if (dist > a.genes.matingRange && dist > b.genes.matingRange) return null;
 
        var childGenes = GeneticsOps.Reproduce(a.genes, b.genes, s);
 
        float cost   = s.offspringEnergyFraction * 0.5f;
        a.energy     = Mathf.Max(0f, a.energy - cost);
        b.energy     = Mathf.Max(0f, b.energy - cost);
 
        a.reproductionCooldown = s.reproductionCooldown;
        b.reproductionCooldown = s.reproductionCooldown;
 
        a.offspringCount++;
        b.offspringCount++;
 
        return childGenes;
    }
 
    public static AnimalState CreateOffspring(
        AnimalState    parentA,
        GeneticProfile childGenes,
        SimulationSettings s,
        int            newId)
    {
        return new AnimalState
        {
            id                   = newId,
            species              = parentA.species,
            genes                = childGenes,
            energy               = s.offspringEnergyFraction,
            hunger               = 0.1f,
            thirst               = 0.1f,
            position             = parentA.position + Random.insideUnitCircle * 0.5f,
            velocity             = Vector2.zero,
            age                  = 0f,
            // La prole inizia con cooldown pieno: non si riproduce subito dopo la nascita
            reproductionCooldown = s.reproductionCooldown,
        };
    }
}