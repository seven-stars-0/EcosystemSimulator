// hungerRate [0.001, 0.03]   default 0.008  — con 0.03 l'animale muore in 33s
// thirstRate [0.001, 0.04]   default 0.012  — più labile dell'energia
// metabolismBase [0, 0.005]  default 0.001  — piccolo, è solo il costo a riposo
// speedEnergyCost [0, 0.02]  default 0.004  — a speed 3 costa 0.012/s
// slopeEnergyCost [0, 0.1]   default 0.02   — scala col gradiente × velocità
// steeringForceScale [0.3, 5] default 1.2   — moltiplicatore globale del vettore
// plantEnergyValue [0.05, 1.5] default 0.25 — un frutto = 25% dell'energia base
// preyEnergyValue [0.1, 3]   default 0.60   — un'intera preda = 60% energia base
// foodHungerRestore [0, 1]   default 0.35   — riduce la fame del 35% per frutto
// waterThirstRestore [0.1, 3] default 0.50  — è per-secondo: 0.5 = -0.15/s
// drinkingRange [1, 15]      default 3.0    — in unità mondo (cellSize=2 → 1.5 celle)
// urgencyMax [1, 8]          default 3.5    — moltiplicatore massimo per urgenza
// offspringEnergyFraction [0.05, 0.5] default 0.25
// reproductionThreshold [0.2, 0.95] default 0.4  — soglia energia per riprodursi
// reproductionCooldown [5, 180s]   default 40s
// mateSeekingBoost [1, 15]  default 6
// mutationRate [0, 0.5]     default 0.04
// mutationStrength [0, 0.5] default 0.10
// plantGrowthRate [0.001, 0.15] default 0.03 — prob/tick/fertilità
// fruitRegrowTimeMin [1, 60s]   default 8s
// fruitRegrowTimeMax [10, 120s] default 40s
// plantMinHeight [0, 0.5]   default 0.25   — sopra la sabbia
// attackRange [0.5, 8]      default 1.8    — in unità mondo
// attackCooldown [0.5, 10s] default 2.5s
// knockbackSpeed [0, 20]    default 6
// separationRadius [0.5, 8] default 2.0
// separationForce [0, 20]   default 5.0
 
using UnityEngine;
 
public class ParameterPanel : MonoBehaviour
{
    // ── Physiology ────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam hungerRate;
    [SerializeField] private SliderParam thirstRate;
    [SerializeField] private SliderParam metabolismBase;
 
    // ── Movement ─────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam speedEnergyCost;
    [SerializeField] private SliderParam slopeEnergyCost;
    [SerializeField] private SliderParam steeringForceScale;
 
    // ── Feeding ───────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam plantEnergyValue;
    [SerializeField] private SliderParam preyEnergyValue;
    [SerializeField] private SliderParam foodHungerRestore;
    [SerializeField] private SliderParam waterThirstRestore;
    [SerializeField] private SliderParam drinkingRange;
    [SerializeField] private SliderParam urgencyMax;
 
    // ── Reproduction ─────────────────────────────────────────────────────────
    [SerializeField] private SliderParam offspringEnergyFraction;
    [SerializeField] private SliderParam reproductionThreshold;
    [SerializeField] private SliderParam reproductionCooldown;
    [SerializeField] private SliderParam mateSeekingBoost;
 
    // ── Genetics ─────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam mutationRate;
    [SerializeField] private SliderParam mutationStrength;
 
    // ── Plants ────────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam plantGrowthRate;
    [SerializeField] private SliderParam fruitRegrowTimeMin;
    [SerializeField] private SliderParam fruitRegrowTimeMax;
    [SerializeField] private SliderParam plantMinHeight;
 
    // ── Predation ────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam attackRange;
    [SerializeField] private SliderParam attackCooldown;
    [SerializeField] private SliderParam knockbackSpeed;
 
    // ── Separation ────────────────────────────────────────────────────────────
    [SerializeField] private SliderParam separationRadius;
    [SerializeField] private SliderParam separationForce;
 
    public void Bind(SimulationSettings s)
    {
        // ── Physiology ────────────────────────────────────────────────────────
        hungerRate     .Setup("Hunger rate",       s.hungerRate,       0.001f, 0.03f,  v => s.hungerRate       = v);
        thirstRate     .Setup("Thirst rate",       s.thirstRate,       0.001f, 0.04f,  v => s.thirstRate       = v);
        metabolismBase .Setup("Base metabolism",   s.metabolismBase,   0f,     0.005f, v => s.metabolismBase   = v);
 
        // ── Movement ──────────────────────────────────────────────────────────
        speedEnergyCost  .Setup("Speed energy cost",    s.speedEnergyCost,    0f,    0.02f, v => s.speedEnergyCost    = v);
        slopeEnergyCost  .Setup("Slope energy cost",    s.slopeEnergyCost,    0f,    0.10f, v => s.slopeEnergyCost    = v);
        steeringForceScale.Setup("Steering force scale", s.steeringForceScale, 0.3f,  5f,   v => s.steeringForceScale = v);  // FIX: era s.slopeEnergyCost
 
        // ── Feeding ───────────────────────────────────────────────────────────
        plantEnergyValue  .Setup("Plant energy value",   s.plantEnergyValue,   0.05f, 1.5f,  v => s.plantEnergyValue   = v);
        preyEnergyValue   .Setup("Prey energy value",    s.preyEnergyValue,    0.1f,  3.0f,  v => s.preyEnergyValue    = v);
        foodHungerRestore .Setup("Food hunger restore",  s.foodHungerRestore,  0f,    1f,    v => s.foodHungerRestore  = v);
        waterThirstRestore.Setup("Water thirst restore", s.waterThirstRestore, 0.1f,  3f,    v => s.waterThirstRestore = v);
        drinkingRange     .Setup("Drinking range",       s.drinkingRange,      1f,    15f,   v => s.drinkingRange      = v);
        urgencyMax        .Setup("Urgency max",          s.urgencyMax,         1f,    8f,    v => s.urgencyMax         = v);
 
        // ── Reproduction ──────────────────────────────────────────────────────
        offspringEnergyFraction.Setup("Offspring energy",     s.offspringEnergyFraction, 0.05f, 0.5f,  v => s.offspringEnergyFraction = v);
        reproductionThreshold  .Setup("Reprod. threshold",    s.reproductionThreshold,   0.2f,  0.95f, v => s.reproductionThreshold   = v);
        reproductionCooldown   .Setup("Reprod. cooldown (s)", s.reproductionCooldown,    5f,    180f,  v => s.reproductionCooldown    = v);
        mateSeekingBoost       .Setup("Mate-seeking boost",   s.mateSeekingBoost,        1f,    15f,   v => s.mateSeekingBoost        = v);
 
        // ── Genetics ──────────────────────────────────────────────────────────
        mutationRate    .Setup("Mutation rate",     s.mutationRate,     0f,    0.5f,  v => s.mutationRate     = v);
        mutationStrength.Setup("Mutation strength", s.mutationStrength, 0f,    0.5f,  v => s.mutationStrength = v);
 
        // ── Plants ────────────────────────────────────────────────────────────
        plantGrowthRate  .Setup("Plant growth rate",    s.plantGrowthRate,    0.001f, 0.15f, v => s.plantGrowthRate    = v);
        fruitRegrowTimeMin.Setup("Fruit regrow min (s)", s.fruitRegrowTimeMin, 1f,     60f,   v => s.fruitRegrowTimeMin  = v);
        fruitRegrowTimeMax.Setup("Fruit regrow max (s)", s.fruitRegrowTimeMax, 10f,    120f,  v => s.fruitRegrowTimeMax  = v);
        plantMinHeight   .Setup("Plant min height",     s.plantMinHeight,     0f,     0.5f,  v => s.plantMinHeight     = v);
 
        // ── Predation ─────────────────────────────────────────────────────────
        attackRange   .Setup("Attack range",    s.attackRange,    0.5f,  8f,   v => s.attackRange    = v);
        attackCooldown.Setup("Attack cooldown", s.attackCooldown, 0.5f,  10f,  v => s.attackCooldown = v);
        knockbackSpeed.Setup("Knockback speed", s.knockbackSpeed, 0f,    20f,  v => s.knockbackSpeed = v);
 
        // ── Separation ────────────────────────────────────────────────────────
        separationRadius.Setup("Separation radius", s.separationRadius, 0.5f, 8f,  v => s.separationRadius = v);
        separationForce .Setup("Separation force",  s.separationForce,  0f,   20f, v => s.separationForce  = v);
    }
}