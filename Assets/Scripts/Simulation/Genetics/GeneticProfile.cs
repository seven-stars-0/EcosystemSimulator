using System;
using UnityEngine;

[Serializable]
public class GeneticProfile
{
    public float w_food = 1.0f;
    public float w_water = 0.8f;
    public float w_social = 0.3f;
    public float w_slope = 0.2f;
    public float w_curiosity = 0.4f;
    public float w_flee = 1.5f;

    [Range(0.5f, 10f)] public float maxSpeed = 3f;
    [Range(2f, 40f)] public float visionRange = 12f;
    [Range(0.3f, 3f)] public float metabolismMult = 1f;

    public const int GENE_COUNT = 9;

    public static readonly string[] GeneNames =
    {
        "w_food", "w_water", "w_social", "w_slope", "w_curiosity", "w_flee",
        "maxSpeed", "visionRange", "metabolismMult"
    };

    public float[] ToArray() => new float[]
    {
        w_food, w_water, w_social, w_slope, w_curiosity, w_flee,
        maxSpeed, visionRange, metabolismMult
    };

    public void FromArray(float[] g)
    {
        w_food = g[0];
        w_water = g[1];
        w_social = g[2];
        w_slope = g[3];
        w_curiosity = g[4];
        w_flee = g[5];
        maxSpeed = g[6];
        visionRange = g[7];
        metabolismMult = g[8];
    }

    public static GeneticProfile RandomForPrey()
    {
        var g = new GeneticProfile();

        // TENDENZE PREDE: Focalizzate sulla sopravvivenza di gruppo e sulla vigilanza panoramica
        g.w_food = Rand(0.8f, 2.0f);   // Meno disperate dei predatori sul cibo, ma costante
        g.w_water = Rand(0.6f, 1.6f);
        g.w_social = Rand(-1.0f, 2.0f);  // Tendenzialmente gregarie (positive), ma possono nascere prede asociali (negative)
        g.w_slope = Rand(-0.4f, 0.2f);  // Preferiscono evitare pendenze ripide per risparmiare energia
        g.w_curiosity = Rand(0.4f, 1.6f);   // Meno inclini a vagare a caso in territori sconosciuti
        g.w_flee = Rand(1.2f, 3.0f);   // Forte istinto di fuga (fondamentale)

        // Statistiche fisiche (con potenziale di surclassamento)
        g.maxSpeed = Rand(2.0f, 6.0f);   // Di base più lente dei predatori, ma una preda al top (6.0) batte un predatore lento
        g.visionRange = Rand(12.0f, 26.0f); // Vista tendenzialmente migliore dei predatori per avvistarli in anticipo
        g.metabolismMult = Rand(0.4f, 1.3f);   // Metabolismo tendenzialmente più efficiente e basso per resistere alle carestie

        return g;
    }

    public static GeneticProfile RandomForPredator()
    {
        var g = new GeneticProfile();

        // TENDENZE PREDATORI: Focalizzati sulla caccia attiva, esplorazione e dominanza fisica
        g.w_food = Rand(1.4f, 2.8f);   // Spinta predatoria/fame estremamente accentuata
        g.w_water = Rand(0.4f, 1.4f);
        g.w_social = Rand(-2.0f, 1.0f);  // Tendenzialmente solitari (negative), ma possono nascere predatori da branco (positive)
        g.w_slope = Rand(-0.2f, 0.4f);  // Sfruttano le pendenze per tracciare il territorio o tendere agguati
        g.w_curiosity = Rand(0.8f, 2.4f);   // Molto curiosi, fondamentale per pattugliare e trovare prede nascoste
        g.w_flee = 0f;

        // Statistiche fisiche (con potenziale di surclassamento)
        g.maxSpeed = Rand(3.0f, 7.0f);   // Di base più veloci per la rincorsa, ma un predatore goffo (3.0) si farà seminare
        g.visionRange = Rand(8.0f, 20.0f);  // Vista più focalizzata e corta rispetto alle prede, ma sovrapponibile
        g.metabolismMult = Rand(0.8f, 1.8f);   // Metabolismo più alto dovuto allo sforzo della caccia attiva

        return g;
    }

    private static float Rand(float min, float max)
        => UnityEngine.Random.Range(min, max);
}