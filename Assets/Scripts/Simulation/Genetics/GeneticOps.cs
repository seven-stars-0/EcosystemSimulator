// 1. CROSSOVER UNIFORME PURO:
//    Ogni gene viene ereditato SECCAMENTE al 50% dal padre O dalla madre.
//    Il Lerp precedente era un "frullatore" che azzerava i tratti estremi:
//    se un genitore ha maxSpeed=6.0 e uno ha maxSpeed=2.0, il figlio
//    aveva quasi sempre ~4.0 → convergenza verso la mediocrità.
//    Con il crossover uniforme, il figlio può ereditare 6.0 OR 2.0,
//    preservando i tratti vantaggiosi e mantenendo diversità.
//
// 2. MUTAZIONE GAUSSIANA (Box-Muller):
//    Sostituisce il delta uniforme Random.Range(-strength, +strength).
//    La distribuzione gaussiana N(0, mutationStrength) rispecchia la
//    biologia: piccole mutazioni molto frequenti, grandi stravolgimenti
//    rarissimi. Con il vecchio range uniforme, una mutazione di ±0.10 era
//    ugualmente probabile di ±0.001, il che è irrealistico.

using UnityEngine;

public static class GeneticsOps
{
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
            // ── Crossover uniforme puro ──────────────────────────────────────
            // 50% di probabilità di prendere il gene da A o da B.
            // NON è una media: il gene è uno dei due valori originali.
            child[i] = Random.value < 0.5f ? a[i] : b[i];

            // ── Mutazione gaussiana (Box-Muller) ─────────────────────────────
            // Genera un delta distribuito normalmente con σ = mutationStrength.
            // Piccole mutazioni sono molto più probabili di grandi stravolgimenti.
            if (Random.value < s.mutationRate)
                child[i] += GaussianSample() * s.mutationStrength;
        }

        var profile = new GeneticProfile();
        profile.FromArray(child);

        // ── Clamp valori con limiti biologici ────────────────────────────────
        profile.w_curiosity = Mathf.Max(0f, profile.w_curiosity);
        profile.maxSpeed = Mathf.Max(0.2f, profile.maxSpeed);
        profile.visionRange = Mathf.Max(1f, profile.visionRange);
        profile.metabolismMult = Mathf.Max(0.1f, profile.metabolismMult);

        return profile;
    }

    /// <summary>
    /// Genera un campione dalla distribuzione normale standard N(0,1)
    /// usando l'algoritmo di Box-Muller.
    /// </summary>
    private static float GaussianSample()
    {
        // Evita log(0): clamp a un valore molto piccolo ma positivo
        float u1 = Mathf.Max(1e-6f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}