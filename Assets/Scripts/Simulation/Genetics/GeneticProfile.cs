using System;

// Tre geni, usati in PerceptionSystem (visionRange) e SteeringSystem (maxSpeed e social)
// Un tempo c'erano molti altri geni (ben 13), ma il codice si era permesso di non funzionare esattamente
// come volevo io, quindi l'ho dovuto punire. Adesso ha imparato la lezione e non causa più problemi
public class GeneticProfile
{
    public float maxSpeed = 3f;
    public float visionRange = 12f;
    public float social = 0.5f;

    public static GeneticProfile RandomForPrey() => new GeneticProfile
    {
        maxSpeed    = Rand(2.5f, 5.5f),
        visionRange = Rand(12f, 22f),
        social      = Rand(-0.5f, 1.5f),   // Prede tendenzialmente gregarie
    };

    public static GeneticProfile RandomForPredator() => new GeneticProfile
    {
        maxSpeed    = Rand(3.0f, 6.0f),
        visionRange = Rand(8f, 16f),
        social      = Rand(-1.5f, 0.5f),  // Predatori tendenzialmente solitari
    };

    public GeneticProfile Clone() => new GeneticProfile
    {
        maxSpeed = maxSpeed, visionRange = visionRange, social = social
    };

    private static float Rand(float a, float b) => UnityEngine.Random.Range(a, b);
}
