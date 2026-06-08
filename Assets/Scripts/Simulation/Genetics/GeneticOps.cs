using UnityEngine;

public static class GeneticsOps
{
    /// <summary>
    /// Crossover uniforme + mutazione gaussiana.
    /// Ogni gene del figlio viene preso a caso da A o B,
    /// poi con probabilità mutationRate viene perturbato di ±mutationStrength.
    /// </summary>
    public static GeneticProfile Reproduce(
        GeneticProfile parentA,
        GeneticProfile parentB,
        SimulationSettings s)
    {
        float[] a = parentA.ToArray();
        float[] b = parentB.ToArray();
        float[] child = new float[GeneticProfile.GENE_COUNT];

        for (int i = 0; i < GeneticProfile.GENE_COUNT; i++)
        {
            // Crossover: media pesata casuale (più variegata del 50/50 puro)
            float t = Random.value;
            child[i] = Mathf.Lerp(a[i], b[i], t);

            // Mutazione
            if (Random.value < s.mutationRate)
            {
                float delta = Random.Range(-s.mutationStrength, s.mutationStrength);
                child[i] += delta;
            }
        }

        var profile = new GeneticProfile();
        profile.FromArray(child);

        // Clamp valori che non possono essere negativi
        profile.w_curiosity = Mathf.Max(0f, profile.w_curiosity);
        profile.maxSpeed = Mathf.Max(0.2f, profile.maxSpeed);
        profile.visionRange = Mathf.Max(1f, profile.visionRange);
        profile.metabolismMult = Mathf.Max(0.1f, profile.metabolismMult);
        profile.reproductionThreshold = Mathf.Clamp(profile.reproductionThreshold, 0.2f, 0.99f);
        profile.bodySize = Mathf.Max(0.3f, profile.bodySize);

        return profile;
    }
}