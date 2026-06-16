using System;

// ═══════════════════════════════════════════════════════════════════════════════
//  Mappatura coefficienti LV → parametri:
//
//   dN/dt  = α·N·(1 - N/K)  -  β·N·P
//   dP/dt  = γ·β·N·P         -  δ·P
//
//   α  (tasso riproduttivo prede)   → reproductionCooldown (↑ cooldown = ↓ α)
//   β  (tasso predazione)           → attackRange, attackCooldown, killChance
//   γ  (efficienza conversione)     → preyEnergyValue
//   δ  (mortalità predatori)        → hungerRate, metabolismBase
//   K  (carrying capacity prede)    → plantGrowthRate + plantCarryingCapacity
// ═══════════════════════════════════════════════════════════════════════════════

[Serializable]
public class SimulationSettings
{
    // ── Physiology (δ) ────────────────────────────────────────────────────────
    // Un animale a riposo con metabolismMult=1 brucia:
    //   energia: metabolismBase/s = 0.00015/s  → riserva base ~1.0 dura ~1.8h sim
    //   fame:    hungerRate/s     = 0.0008/s   → da 0 a 1 in ~1250s (~20min sim)
    //   sete:    thirstRate/s     = 0.0012/s   → da 0 a 1 in ~833s  (~14min sim)
    // A velocità massima (6 u/s) si aggiunge speedEnergyCost*speed = 0.0015/s.
    public float hungerRate = 0.0008f;   // ↓ era 0.008  (÷10)
    public float thirstRate = 0.0012f;   // ↓ era 0.012  (÷10)
    public float metabolismBase = 0.00015f;  // ↓ era 0.001  (÷~7)

    // ── Movement ──────────────────────────────────────────────────────────────
    public float speedEnergyCost = 0.00025f;  // ↓ era 0.004  (÷16)
    public float slopeEnergyCost = 0.002f;    // ↓ era 0.02   (÷10)
    public float steeringForceScale = 1.2f;

    // ── Feeding (γ) ───────────────────────────────────────────────────────────
    // Con il metabolismo più lento, un frutto deve comunque essere "sostanzioso":
    //   plantEnergyValue 0.20 = +20% energia per frutto (era 0.25, ora proporzionato)
    //   preyEnergyValue  0.55 = un kill copre ~55% energia predatore (era 0.60)
    //   foodHungerRestore   : riduce fame del 40% per frutto (era 0.35)
    public float plantEnergyValue = 0.20f;
    public float preyEnergyValue = 0.55f;
    public float foodHungerRestore = 0.40f;
    public float waterThirstRestore = 0.50f;
    public float drinkingRange = 3f;

    // ── Reproduction (α) ──────────────────────────────────────────────────────
    // reproductionCooldown 120s: con timeScale=1 → ~2 min reali tra nascite.
    // Con timeScale=5 → 24s reali → generazione osservabile ma non frenetica.
    public float offspringEnergyFraction = 0.30f;     // ↑ lieve (cucciolo più robusto)
    public float reproductionThreshold = 0.55f;     // ↑ era 0.40 (serve più energia)
    public float reproductionCooldown = 120f;      // ↑ era 40s  (×3)
    public float mateSeekingBoost = 6f;

    // ── Genetics ──────────────────────────────────────────────────────────────
    public float mutationRate = 0.04f;
    public float mutationStrength = 0.10f;

    // ── Plants (K – carrying capacity) ───────────────────────────────────────
    // plantGrowthRate ridotto: evita saturazione istantanea dopo boom.
    // plantCarryingCapacityFraction: soglia oltre la quale la crescita crolla.
    public float plantGrowthRate = 0.012f;  // ↓ era 0.03 (÷2.5)
    public float plantCarryingCapacityFraction = 0.35f; // NUOVO: tetto al 35% celle
    public float fruitRegrowTimeMin = 20f;     // ↑ era 8s
    public float fruitRegrowTimeMax = 80f;     // ↑ era 40s
    public float plantMinHeight = 0.25f;

    // ── Urgency ───────────────────────────────────────────────────────────────
    public float urgencyMax = 3.5f;

    // ── Predation (β) ─────────────────────────────────────────────────────────
    // killChance: probabilità base di successo attacco (parametro β in LV).
    //   40% = predatore fallisce il 60% degli attacchi → pressione ridotta.
    // handlingTime: secondi in cui il predatore è "occupato" dopo un kill.
    //   Durante l'handling il predatore ignora altre prede (saturazione Type II).
    // attackCooldown (miss): mini-cooldown se l'attacco fallisce.
    public float attackRange = 1.8f;
    public float attackCooldown = 3.0f;      // ↑ era 2.5s
    public float knockbackSpeed = 7f;
    public float killChance = 0.42f;     // NUOVO: β esplicito (42%)
    public float handlingTime = 9f;        // NUOVO: Holling Type II
    public float missStunDuration = 1.2f;      // NUOVO: cooldown su miss

    // ── Separation ────────────────────────────────────────────────────────────
    public float separationRadius = 2.0f;
    public float separationForce = 5.0f;

    // ── Micro-Immigration (safety net) ────────────────────────────────────────
    // Se una specie scende sotto immigrationThreshold individui,
    // ogni immigrationInterval secondi viene spawnato 1 individuo al bordo mappa.
    public int immigrationThreshold = 3;         // NUOVO
    public float immigrationInterval = 60f;       // NUOVO: 60s reali di simulazione
}