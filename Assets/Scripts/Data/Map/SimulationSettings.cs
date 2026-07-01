using System;
using System.Reflection;
using UnityEngine;

// ============================================================================
//  SimulationSettings  -  Parametri dell'ecologia a energia.
// ----------------------------------------------------------------------------
//  TEMPO STIRATO 3x: tutti i parametri-tempo sono scalati di un fattore 3
//  (consumi /3, durate *3) -> stessa dinamica, ma vita piu' lunga e oscillazioni
//  piu' ampie. Energie-per-pasto, soglie, costi, capacita' e movimento NON sono
//  tempi e restano invariati (la forma del ciclo non cambia, solo la sua durata).
// ============================================================================

[Serializable]
public class SimulationSettings
{
    // -- Prede: alimentazione e riproduzione -------------------------------
    public float preyEnergyPerPlant = 0.35f;   // energia/pasto (NON tempo)
    public float preyReproThreshold = 0.55f;   // livello energia (NON tempo)
    public float preyReproCost      = 0.25f;   // energia (NON tempo)
    public float preyReproCooldown  = 30f;     // TEMPO  (era 10, *3)
    public float preyCarryingCapacity = 150f;
    public float preyCapPredatorSensitivity = 0f;

    // -- Predazione: rifugio spaziale + handling time ----------------------
    public float attackRange  = 2.0f;
    public float killChance   = 0.25f;
    public float handlingTime = 36f;           // TEMPO  (era 12, *3)

    // -- Predatori ---------------------------------------------------------
    public float predatorEnergyPerPrey  = 0.55f;
    public float predatorReproThreshold = 0.60f;
    public float predatorReproCost      = 0.40f;
    public float predatorReproCooldown  = 54f;     // TEMPO (era 18, *3)

    public float predatorMetabolicDrain = 0.0027f; // energia/TEMPO (era 0.008, /3)
    public float preyMetabolicDrain     = 0.0005f; // energia/TEMPO (era 0.0015, /3)
    public float hungerRate             = 0.0005f; // fame/TEMPO    (era 0.0015, /3)

    public float predatorFoodRatio = 3.0f;
    public float predatorInterference = 0.5f;

    // -- Mortalita' predatori da SCARSITA' di prede (tipo Leslie-Gower).
    //    Dipende dal rapporto R = prede/predatori: se R scende sotto ComfortRatio
    //    (troppi predatori per le prede disponibili) i predatori iniziano a morire,
    //    con prob. crescente man mano che R -> 0. Sopra ComfortRatio: nessuna morte
    //    extra (mangiano e si riproducono). E' STOCASTICA per-individuo -> niente
    //    coorti sincronizzate (niente "spalle"), ed e' AUTO-LIMITANTE: appena i
    //    predatori calano, R risale e il termine si spegne -> non si estinguono mai
    //    se ci sono prede.
    public float predatorScarcityMortality = 0.02f;  // prob/s di morte a R=0 (max)
    public float predatorComfortRatio      = 1.5f;   // prede/predatore sopra cui niente morte extra

    public float energyMax = 1.0f;
    public float offspringEnergy = 0.45f;   // energia di partenza della prole (sotto soglia di riproduzione)

    // -- Aspetto (scala dei prefab per specie) --
    public float preyScale = 1f;
    public float predatorScale = 1.2f;

    // -- Evoluzione: 3 geni (maxSpeed, visionRange, social) ----------------
    public float mutationRate = 0.05f;
    public float mutationStrength = 0.10f;
    public float speedMin = 1.5f, speedMax = 7f;
    public float visionMin = 6f, visionMax = 24f;
    public float socialMin = -2f, socialMax = 2.5f;

    // -- Piante (cibo visivo) ----------------------------------------------
    public float plantGrowthRate = 0.01f;      // crescita/TEMPO (era 0.03, /3)
    public float plantCarryingCapacityFraction = 0.35f;
    public float fruitRegrowTime = 48f;        // TEMPO (era 16, *3)
    public float plantMinHeight = 0.25f;

    // -- Logging --
    public float logSampleInterval = 1f;   // secondi tra i campioni del CSV (1..120)

    public SimulationSettings Clone() => (SimulationSettings)MemberwiseClone();

    public void CopyFrom(SimulationSettings other)
    {
        foreach (FieldInfo f in typeof(SimulationSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            f.SetValue(this, f.GetValue(other));
    }
}
