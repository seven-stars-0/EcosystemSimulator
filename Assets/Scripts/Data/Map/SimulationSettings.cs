using System;
 
[Serializable]
public class SimulationSettings
{
    // ── Physiology ────────────────────────────────────────────────────────────
    public float hungerRate              = 0.008f;
    public float thirstRate              = 0.012f;
    public float metabolismBase          = 0.001f;
 
    // ── Movement ─────────────────────────────────────────────────────────────
    public float speedEnergyCost         = 0.004f;
    public float slopeEnergyCost         = 0.02f;
    public float steeringForceScale      = 1.2f;
 
    // ── Feeding ───────────────────────────────────────────────────────────────
    public float plantEnergyValue        = 0.25f;
    public float preyEnergyValue         = 0.60f;
    public float foodHungerRestore       = 0.35f;
    public float waterThirstRestore      = 0.50f;
    public float drinkingRange           = 3f;
 
    // ── Reproduction ─────────────────────────────────────────────────────────
    public float offspringEnergyFraction = 0.25f;
    public float reproductionThreshold   = 0.4f;
    public float reproductionCooldown    = 40f;
    public float mateSeekingBoost        = 6f;
 
    // ── Genetics ─────────────────────────────────────────────────────────────
    public float mutationRate            = 0.04f;
    public float mutationStrength        = 0.10f;
 
    // ── Plants ────────────────────────────────────────────────────────────────
    public float plantGrowthRate         = 0.03f;
    public float fruitRegrowTimeMin      = 8f;
    public float fruitRegrowTimeMax      = 40f;
    public float plantMinHeight          = 0.25f;
 
    // ── Urgency ───────────────────────────────────────────────────────────────
    public float urgencyMax              = 3.5f;
 
    // ── Predation ────────────────────────────────────────────────────────────
    public float attackRange             = 1.8f;
 
    /// <summary>Secondi prima che un predatore possa attaccare di nuovo.</summary>
    public float attackCooldown          = 2.5f;
 
    /// <summary>Velocità di knockback applicata alla preda al momento dell'attacco.</summary>
    public float knockbackSpeed          = 6f;
 
    // ── Separation ────────────────────────────────────────────────────────────
    public float separationRadius        = 2.0f;
    public float separationForce         = 5.0f;
 
    public SimulationSettings Clone() => (SimulationSettings)MemberwiseClone();
}