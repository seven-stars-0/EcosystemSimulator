using UnityEngine;

public class ParameterPanel : MonoBehaviour
{
    // ── Physiology (Metabolismo rallentato) ──────────────────────────────────
    [SerializeField] private SliderParam hungerRate;
    [SerializeField] private SliderParam thirstRate;
    [SerializeField] private SliderParam metabolismBase;

    // ── Movement ─────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam speedEnergyCost;
    [SerializeField] private SliderParam slopeEnergyCost;
    [SerializeField] private SliderParam steeringForceScale;

    // ── Feeding ──────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam plantEnergyValue;
    [SerializeField] private SliderParam preyEnergyValue;
    [SerializeField] private SliderParam foodHungerRestore;
    [SerializeField] private SliderParam waterThirstRestore;
    [SerializeField] private SliderParam drinkingRange;
    [SerializeField] private SliderParam urgencyMax;

    // ── Reproduction (Tempi dilatati) ────────────────────────────────────────
    [SerializeField] private SliderParam offspringEnergyFraction;
    [SerializeField] private SliderParam reproductionThreshold;
    [SerializeField] private SliderParam reproductionCooldown;
    [SerializeField] private SliderParam mateSeekingBoost;

    // ── Genetics ─────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam mutationRate;
    [SerializeField] private SliderParam mutationStrength;

    // ── Plants (Carrying Capacity inclusa) ───────────────────────────────────
    [SerializeField] private SliderParam plantGrowthRate;
    [SerializeField] private SliderParam plantCarryingCapacityFraction;
    [SerializeField] private SliderParam fruitRegrowTimeMin;
    [SerializeField] private SliderParam fruitRegrowTimeMax;
    [SerializeField] private SliderParam plantMinHeight;

    // ── Predation (Predatore fallibile di Lotka-Volterra) ────────────────────
    [SerializeField] private SliderParam attackRange;
    [SerializeField] private SliderParam attackCooldown;
    [SerializeField] private SliderParam knockbackSpeed;
    [SerializeField] private SliderParam killChance;
    [SerializeField] private SliderParam handlingTime;
    [SerializeField] private SliderParam missStunDuration;

    // ── Separation ───────────────────────────────────────────────────────────
    [SerializeField] private SliderParam separationRadius;
    [SerializeField] private SliderParam separationForce;

    // ── Micro-Immigration (Safety Net) ───────────────────────────────────────
    [SerializeField] private SliderParam immigrationThreshold;
    [SerializeField] private SliderParam immigrationInterval;

    public void Bind(SimulationSettings s)
    {
        // ── Physiology 
        // I nuovi consumi sono microscopici, i range sono stati scalati verso il basso
        hungerRate.Setup("Hunger rate", s.hungerRate, 0.0001f, 0.005f, v => s.hungerRate = v);
        thirstRate.Setup("Thirst rate", s.thirstRate, 0.0001f, 0.008f, v => s.thirstRate = v);
        metabolismBase.Setup("Base metabolism", s.metabolismBase, 0f, 0.002f, v => s.metabolismBase = v);

        // ── Movement 
        speedEnergyCost.Setup("Speed energy cost", s.speedEnergyCost, 0f, 0.005f, v => s.speedEnergyCost = v);
        slopeEnergyCost.Setup("Slope energy cost", s.slopeEnergyCost, 0f, 0.02f, v => s.slopeEnergyCost = v);
        steeringForceScale.Setup("Steering force scale", s.steeringForceScale, 0.3f, 5f, v => s.steeringForceScale = v);

        // ── Feeding 
        plantEnergyValue.Setup("Plant energy value", s.plantEnergyValue, 0.05f, 1.0f, v => s.plantEnergyValue = v);
        preyEnergyValue.Setup("Prey energy value", s.preyEnergyValue, 0.1f, 2.0f, v => s.preyEnergyValue = v);
        foodHungerRestore.Setup("Food hunger restore", s.foodHungerRestore, 0f, 1f, v => s.foodHungerRestore = v);
        waterThirstRestore.Setup("Water thirst restore", s.waterThirstRestore, 0.1f, 2f, v => s.waterThirstRestore = v);
        drinkingRange.Setup("Drinking range", s.drinkingRange, 1f, 15f, v => s.drinkingRange = v);
        urgencyMax.Setup("Urgency max", s.urgencyMax, 1f, 8f, v => s.urgencyMax = v);

        // ── Reproduction 
        offspringEnergyFraction.Setup("Offspring energy", s.offspringEnergyFraction, 0.05f, 0.5f, v => s.offspringEnergyFraction = v);
        reproductionThreshold.Setup("Reprod. threshold", s.reproductionThreshold, 0.2f, 0.95f, v => s.reproductionThreshold = v);
        reproductionCooldown.Setup("Reprod. cooldown (s)", s.reproductionCooldown, 10f, 300f, v => s.reproductionCooldown = v);
        mateSeekingBoost.Setup("Mate-seeking boost", s.mateSeekingBoost, 1f, 15f, v => s.mateSeekingBoost = v);

        // ── Genetics 
        mutationRate.Setup("Mutation rate", s.mutationRate, 0f, 0.5f, v => s.mutationRate = v);
        mutationStrength.Setup("Mutation strength", s.mutationStrength, 0f, 0.5f, v => s.mutationStrength = v);

        // ── Plants 
        plantGrowthRate.Setup("Plant growth rate", s.plantGrowthRate, 0.001f, 0.10f, v => s.plantGrowthRate = v);
        plantCarryingCapacityFraction.Setup("Max plant coverage", s.plantCarryingCapacityFraction, 0.05f, 1f, v => s.plantCarryingCapacityFraction = v);
        fruitRegrowTimeMin.Setup("Fruit regrow min (s)", s.fruitRegrowTimeMin, 1f, 120f, v => s.fruitRegrowTimeMin = v);
        fruitRegrowTimeMax.Setup("Fruit regrow max (s)", s.fruitRegrowTimeMax, 10f, 300f, v => s.fruitRegrowTimeMax = v);
        plantMinHeight.Setup("Plant min height", s.plantMinHeight, 0f, 0.5f, v => s.plantMinHeight = v);

        // ── Predation 
        attackRange.Setup("Attack range", s.attackRange, 0.5f, 8f, v => s.attackRange = v);
        attackCooldown.Setup("Attack cooldown", s.attackCooldown, 0.5f, 10f, v => s.attackCooldown = v);
        knockbackSpeed.Setup("Knockback speed", s.knockbackSpeed, 0f, 20f, v => s.knockbackSpeed = v);
        killChance.Setup("Base kill chance", s.killChance, 0.05f, 1f, v => s.killChance = v);
        handlingTime.Setup("Handling time (s)", s.handlingTime, 1f, 30f, v => s.handlingTime = v);
        missStunDuration.Setup("Miss stun (s)", s.missStunDuration, 0f, 10f, v => s.missStunDuration = v);

        // ── Separation 
        separationRadius.Setup("Separation radius", s.separationRadius, 0.5f, 8f, v => s.separationRadius = v);
        separationForce.Setup("Separation force", s.separationForce, 0f, 20f, v => s.separationForce = v);

        // ── Micro-Immigration
        immigrationThreshold.SetupInt("Immigr. threshold", s.immigrationThreshold, 0, 20, v => s.immigrationThreshold = v);
        immigrationInterval.Setup("Immigr. interval (s)", s.immigrationInterval, 10f, 300f, v => s.immigrationInterval = v);
    }
}