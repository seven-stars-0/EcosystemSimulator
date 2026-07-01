using UnityEngine;
using UnityEngine.UI;

public class ParameterPanel : MonoBehaviour
{
    [Header("Reset")]
    [SerializeField] private Button resetButton;

    [Header("Oscillazioni (cap dinamici)")]
    [SerializeField] private SliderParam preyCarryingCapacity;
    [SerializeField] private SliderParam preyCapPredatorSensitivity;
    [SerializeField] private SliderParam predatorFoodRatio;
    [SerializeField] private SliderParam predatorInterference;

    [Header("Logging")]
    [SerializeField] private SliderParam logSampleInterval;

    [Header("Prede - crescita")]
    [SerializeField] private SliderParam preyEnergyPerPlant;
    [SerializeField] private SliderParam preyReproThreshold;
    [SerializeField] private SliderParam preyReproCost;
    [SerializeField] private SliderParam preyReproCooldown;

    [Header("Predazione")]
    [SerializeField] private SliderParam attackRange;
    [SerializeField] private SliderParam killChance;
    [SerializeField] private SliderParam handlingTime;

    [Header("Predatori - riproduzione")]
    [SerializeField] private SliderParam predatorEnergyPerPrey;
    [SerializeField] private SliderParam predatorReproThreshold;
    [SerializeField] private SliderParam predatorReproCost;
    [SerializeField] private SliderParam predatorReproCooldown;

    [Header("Metabolismo / fame")]
    [SerializeField] private SliderParam predatorMetabolicDrain;
    [SerializeField] private SliderParam preyMetabolicDrain;
    [SerializeField] private SliderParam hungerRate;
    [SerializeField] private SliderParam energyMax;

    [Header("Genetica (3 geni)")]
    [SerializeField] private SliderParam mutationRate;
    [SerializeField] private SliderParam mutationStrength;
    [SerializeField] private SliderParam speedMin;
    [SerializeField] private SliderParam speedMax;
    [SerializeField] private SliderParam visionMin;
    [SerializeField] private SliderParam visionMax;
    [SerializeField] private SliderParam socialMin;
    [SerializeField] private SliderParam socialMax;

    [Header("Piante")]
    [SerializeField] private SliderParam plantGrowthRate;
    [SerializeField] private SliderParam plantCarryingCapacityFraction;
    [SerializeField] private SliderParam fruitRegrowTime;
    [SerializeField] private SliderParam plantMinHeight;

    private SimulationSettings _bound;

    public void Bind(SimulationSettings s)
    {
        _bound = s;

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetToDefaults);
        }

        // Oscillazioni (cap dinamici)
        preyCarryingCapacity.Setup("Prey carrying cap K0", s.preyCarryingCapacity, 5f, 400f, v => s.preyCarryingCapacity = v);
        preyCapPredatorSensitivity.Setup("Prey cap sensitivity", s.preyCapPredatorSensitivity, 0f, 0.2f, v => s.preyCapPredatorSensitivity = v);
        predatorFoodRatio.Setup("Pred food ratio (P~r*N)", s.predatorFoodRatio, 0.3f, 5f, v => s.predatorFoodRatio = v);
        predatorInterference.Setup("Pred interference", s.predatorInterference, 0f, 2f, v => s.predatorInterference = v);
        logSampleInterval.Setup("Log sample interval (s)", s.logSampleInterval, 1f, 120f, v => s.logSampleInterval = v);

        // Prede - crescita
        preyEnergyPerPlant.Setup("Prey energy / plant", s.preyEnergyPerPlant, 0.05f, 1f, v => s.preyEnergyPerPlant = v);
        preyReproThreshold.Setup("Prey repro threshold", s.preyReproThreshold, 0.3f, 1f, v => s.preyReproThreshold = v);
        preyReproCost.Setup("Prey repro cost", s.preyReproCost, 0.1f, 0.9f, v => s.preyReproCost = v);
        preyReproCooldown.Setup("Prey repro cooldown (s)", s.preyReproCooldown, 5f, 300f, v => s.preyReproCooldown = v);

        // Predazione
        attackRange.Setup("Attack range", s.attackRange, 0.5f, 8f, v => s.attackRange = v);
        killChance.Setup("Kill chance", s.killChance, 0.02f, 1f, v => s.killChance = v);
        handlingTime.Setup("Handling time (s)", s.handlingTime, 0.5f, 90f, v => s.handlingTime = v);

        // Predatori - riproduzione
        predatorEnergyPerPrey.Setup("Pred energy / prey", s.predatorEnergyPerPrey, 0.1f, 1f, v => s.predatorEnergyPerPrey = v);
        predatorReproThreshold.Setup("Pred repro threshold", s.predatorReproThreshold, 0.3f, 1f, v => s.predatorReproThreshold = v);
        predatorReproCost.Setup("Pred repro cost", s.predatorReproCost, 0.1f, 0.9f, v => s.predatorReproCost = v);
        predatorReproCooldown.Setup("Pred repro cooldown (s)", s.predatorReproCooldown, 5f, 300f, v => s.predatorReproCooldown = v);

        // Metabolismo / fame
        predatorMetabolicDrain.Setup("Pred metabolic drain", s.predatorMetabolicDrain, 0.001f, 0.15f, v => s.predatorMetabolicDrain = v);
        preyMetabolicDrain.Setup("Prey metabolic drain", s.preyMetabolicDrain, 0.0001f, 0.02f, v => s.preyMetabolicDrain = v);
        hungerRate.Setup("Hunger rate", s.hungerRate, 0.0001f, 0.02f, v => s.hungerRate = v);
        energyMax.Setup("Energy cap", s.energyMax, 0.5f, 2f, v => s.energyMax = v);

        // Genetica
        mutationRate.Setup("Mutation rate", s.mutationRate, 0f, 0.5f, v => s.mutationRate = v);
        mutationStrength.Setup("Mutation strength", s.mutationStrength, 0f, 0.5f, v => s.mutationStrength = v);
        speedMin.Setup("Speed min", s.speedMin, 0.5f, 6f, v => s.speedMin = v);
        speedMax.Setup("Speed max", s.speedMax, 2f, 12f, v => s.speedMax = v);
        visionMin.Setup("Vision min", s.visionMin, 2f, 20f, v => s.visionMin = v);
        visionMax.Setup("Vision max", s.visionMax, 8f, 40f, v => s.visionMax = v);
        socialMin.Setup("Social min", s.socialMin, -3f, 0f, v => s.socialMin = v);
        socialMax.Setup("Social max", s.socialMax, 0f, 4f, v => s.socialMax = v);

        // Piante
        plantGrowthRate.Setup("Plant growth rate", s.plantGrowthRate, 0.001f, 0.1f, v => s.plantGrowthRate = v);
        plantCarryingCapacityFraction.Setup("Max plant coverage", s.plantCarryingCapacityFraction, 0.05f, 1f, v => s.plantCarryingCapacityFraction = v);
        fruitRegrowTime.Setup("Fruit regrow (s)", s.fruitRegrowTime, 2f, 200f, v => s.fruitRegrowTime = v);
        plantMinHeight.Setup("Plant min height", s.plantMinHeight, 0f, 0.5f, v => s.plantMinHeight = v);
    }

    public void ResetToDefaults()
    {
        if (_bound == null) return;
        UIManager.Instance.ShowConfirm(
            "Reset all simulation parameters to their default values?",
            onConfirm: () =>
            {
                _bound.CopyFrom(new SimulationSettings());
                Bind(_bound);
            });
    }
}
