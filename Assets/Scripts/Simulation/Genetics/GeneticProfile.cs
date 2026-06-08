using System;
using UnityEngine;
 
[Serializable]
public class GeneticProfile
{
    public float w_food      = 1.0f;
    public float w_water     = 0.8f;
    public float w_social    = 0.3f;
    public float w_slope     = 0.2f;
    public float w_curiosity = 0.4f;
    public float w_flee      = 1.5f;
 
    [Range(0.5f,  10f)] public float maxSpeed             = 3f;
    [Range(2f,    40f)] public float visionRange          = 12f;
    [Range(0.3f,   3f)] public float metabolismMult       = 1f;
    [Range(0.3f,   1f)] public float reproductionThreshold = 0.70f;
    [Range(0.5f,   8f)] public float matingRange          = 2f;
    [Range(0.5f, 2.5f)] public float bodySize             = 1f;
 
    public const int GENE_COUNT = 11;
 
    public static readonly string[] GeneNames =
    {
        "w_food", "w_water", "w_social", "w_slope", "w_curiosity", "w_flee",
        "maxSpeed", "visionRange", "metabolismMult", "reproductionThreshold", "bodySize"
    };
 
    public float[] ToArray() => new float[]
    {
        w_food, w_water, w_social, w_slope, w_curiosity, w_flee,
        maxSpeed, visionRange, metabolismMult, reproductionThreshold, bodySize
    };
 
    public void FromArray(float[] g)
    {
        w_food                = g[0];
        w_water               = g[1];
        w_social              = g[2];
        w_slope               = g[3];
        w_curiosity           = g[4];
        w_flee                = g[5];
        maxSpeed              = g[6];
        visionRange           = g[7];
        metabolismMult        = g[8];
        reproductionThreshold = g[9];
        bodySize              = g[10];
    }
 
    public float EffectiveMaxSpeed => maxSpeed / Mathf.Pow(bodySize, 0.4f);
    public float EnergyMax         => 1f + (bodySize - 1f) * 0.5f;
 
    private static GeneticProfile SharedBase()
    {
        var g = new GeneticProfile();
        g.w_water               = Rand(0.1f,  1.5f);
        g.w_social              = Rand(-2.0f, 1.5f);   // più varianza verso asociale
        g.w_slope               = Rand(-0.6f, 0.6f);
        g.w_curiosity           = Rand(0.5f,  2.5f);   // più determinante
        g.maxSpeed              = Rand(2.0f,  6.5f);
        g.visionRange           = Rand(6f,    24f);
        g.metabolismMult        = Rand(0.5f,  1.8f);
        g.reproductionThreshold = Rand(0.45f, 0.80f);
        g.matingRange           = Rand(1f,    4f);
        g.bodySize              = Rand(0.7f,  1.8f);
        return g;
    }
 
    public static GeneticProfile RandomForPrey()
    {
        var g    = SharedBase();
        g.w_food = Rand(0.6f, 2.0f);
        g.w_flee = Rand(0.8f, 2.5f);
        return g;
    }
 
    public static GeneticProfile RandomForPredator()
    {
        var g    = SharedBase();
        g.w_food = Rand(0.8f, 2.5f);
        g.w_flee = 0f;
        return g;
    }
 
    private static float Rand(float min, float max)
        => UnityEngine.Random.Range(min, max);
}