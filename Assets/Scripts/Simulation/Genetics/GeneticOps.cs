using UnityEngine;

public static class GeneticOps
{
    public static GeneticProfile Mutate(GeneticProfile parent, SimulationSettings s)
    {
        var child = parent.Clone();

        // Lo span serve per uniformare la mutationStrength in base al range di valori che il gene può assumere
        // Questo perché l'utente può scegliere, ad esempio, un range piccolissimo per social e grandissimo per speed;
        // se usassero tutte solo mutationStrength allora ci vorrebbe tantissimo per avere un cambiamento significativo in speed, e pochissimo per social
        float speedSpan  = s.speedMax  - s.speedMin;
        float visionSpan = s.visionMax - s.visionMin;
        float socialSpan = s.socialMax - s.socialMin;

        // Mutazioni a caso
        if (Random.value < s.mutationRate) child.maxSpeed    += Gaussian() * s.mutationStrength * speedSpan;
        if (Random.value < s.mutationRate) child.visionRange += Gaussian() * s.mutationStrength * visionSpan;
        if (Random.value < s.mutationRate) child.social      += Gaussian() * s.mutationStrength * socialSpan;

        // Fa il clamp dei valori in base alle impostazioni inserite dall'utente
        child.maxSpeed    = Mathf.Clamp(child.maxSpeed,    s.speedMin,  s.speedMax);
        child.visionRange = Mathf.Clamp(child.visionRange, s.visionMin, s.visionMax);
        child.social      = Mathf.Clamp(child.social,      s.socialMin, s.socialMax);
        return child;
    }

    // Box-Muller per gaussiana per variabili Unif[0,1]
    // u1 non può essere 0, perché log(0) = -inf
    private static float Gaussian()
    {
        float u1 = Mathf.Max(1e-6f, Random.value);
        float u2 = Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
