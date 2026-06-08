using UnityEngine;
 
public static class MetabolismSystem
{
    public static void Tick(AnimalState state, float slope, SimulationSettings s, float dt)
    {
        float speed = state.Speed;
 
        float baseCost  = s.metabolismBase * state.genes.metabolismMult;
        float speedCost = speed * s.speedEnergyCost;
        float slopeCost = slope * s.slopeEnergyCost * speed;
 
        state.energy = Mathf.Max(0f, state.energy - (baseCost + speedCost + slopeCost) * dt);
 
        state.hunger = Mathf.Clamp01(state.hunger + s.hungerRate * state.genes.metabolismMult * dt);
        state.thirst = Mathf.Clamp01(state.thirst + s.thirstRate * state.genes.metabolismMult * dt);
 
        if (state.hunger > 0.8f)
            state.energy = Mathf.Max(0f, state.energy - (state.hunger - 0.8f) * 0.5f * dt);
        if (state.thirst > 0.8f)
            state.energy = Mathf.Max(0f, state.energy - (state.thirst - 0.8f) * 0.5f * dt);
 
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