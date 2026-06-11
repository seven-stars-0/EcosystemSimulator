using UnityEngine;
 
public static class MetabolismSystem
{
    public static void Tick(AnimalState state, float slope, SimulationSettings s, float dt)
    {
        float speed = state.Speed;
 
        float baseCost  = s.metabolismBase * state.genes.metabolismMult;
        float speedCost = speed * s.speedEnergyCost;
        float slopeCost = slope * s.slopeEnergyCost * speed;

        float totalEffort = baseCost + speedCost + slopeCost;

        state.energy = Mathf.Max(0f, state.energy - totalEffort * dt);


        float hungerIncrease = s.hungerRate * state.genes.metabolismMult * dt;
        float thirstIncrease = s.thirstRate * state.genes.metabolismMult * dt;

        // Influenza della velocità dell'animale fame e sete
        hungerIncrease += speedCost * 0.4f * dt;
        thirstIncrease += speedCost * 0.4f * dt;

        state.hunger = Mathf.Clamp01(state.hunger + hungerIncrease);
        state.thirst = Mathf.Clamp01(state.thirst + thirstIncrease);

        // Degrado da fame/sete critica (Feedback Negativo)
        if (state.hunger > 0.8f)
            state.energy -= (state.hunger - 0.8f) * 0.5f * dt;
        if (state.thirst > 0.8f)
            state.energy -= (state.thirst - 0.8f) * 0.5f * dt;

        if (state.reproductionCooldown > 0f) state.reproductionCooldown -= dt;
        if (state.attackCooldown       > 0f) state.attackCooldown       -= dt;
 
        state.age += dt;
    }
 
    public static bool EatFruit(AnimalState state, SimulationSettings s)
    {
        state.hunger = Mathf.Max(0f, state.hunger - s.foodHungerRestore);
        state.energy = Mathf.Min(state.genes.EnergyMax, state.energy + s.plantEnergyValue);
        return true;
    }
 
    public static void EatPrey(AnimalState predator, SimulationSettings s)
    {
        predator.hunger = Mathf.Max(0f, predator.hunger - s.foodHungerRestore * 2f);
        predator.energy = Mathf.Min(predator.genes.EnergyMax, predator.energy + s.preyEnergyValue);
    }
 
    public static void Drink(AnimalState state, SimulationSettings s, float dt)
    {
        state.thirst = Mathf.Max(0f, state.thirst - 0.3f * dt * s.waterThirstRestore);
    }
}