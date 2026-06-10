using System.Collections.Generic;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    public static SimulationRunner Instance { get; private set; }

    [Header("Prefabs animali")]
    public GameObject preyPrefab;
    public GameObject predatorPrefab;

    [Header("Container")]
    public Transform animalContainer;

    [Range(0.5f, 10f)] public float timeScale = 1f;
    public bool paused = false;

    private WorldGrid _grid;
    private RenderConfig _cfg;
    private SimulationSettings _settings;
    private PlantManager _plants;

    private readonly List<Animal> _animals = new();
    private readonly List<Animal> _toAdd = new();
    private readonly List<Animal> _toRemove = new();
    private SpatialGrid<Animal> _spatial;
    private readonly List<Animal> _queryBuf = new();

    private int _nextId;

    public int MaxPreyCount { get; private set; }
    public int MaxPredatorCount { get; private set; }
    public int PreyCount => _preyCount;
    public int PredatorCount => _predatorCount;
    public int PlantCount => _plants?.ActivePlantCount ?? 0;
    public float ElapsedTime { get; private set; }

    private int _preyCount;
    private int _predatorCount;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartSimulation(MapData data, PlantManager plants)
    {
        _grid = data.grid;
        _cfg = WorldSession.Instance.Renderer.config;
        _settings = data.simulationSettings;
        _plants = plants;
        _nextId = 0;
        ElapsedTime = 0f;
        _preyCount = 0;
        _predatorCount = 0;
        MaxPreyCount = 0;
        MaxPredatorCount = 0;

        _spatial = new SpatialGrid<Animal>(Mathf.Max(_settings.drinkingRange, 5f));

        plants.Initialize(_grid, _settings, _cfg, data.spawnEntries);

        _animals.Clear();
        if (data.spawnEntries != null)
            foreach (var entry in data.spawnEntries)
                if (entry.type == SpawnType.Prey || entry.type == SpawnType.Predator)
                    SpawnAnimalFromEntry(entry);

        paused = false;
        Debug.Log($"[SimulationRunner] Started: {_preyCount} prey, {_predatorCount} predators.");
    }

    private void Update()
    {
        if (paused || _grid == null) return;

        float dt = Time.deltaTime * timeScale;
        ElapsedTime += dt;

        _plants.Tick(dt);

        _spatial.Clear();
        foreach (var a in _animals)
            if (a != null && a.IsAlive) _spatial.Insert(a.State.position, a);

        foreach (var a in _animals)
        {
            if (a == null || !a.IsAlive) { _toRemove.Add(a); continue; }
            TickAnimal(a, dt);
        }

        foreach (var n in _toAdd) AddAnimal(n);
        foreach (var d in _toRemove) RemoveAnimal(d);
        _toAdd.Clear();
        _toRemove.Clear();

        if (_preyCount > MaxPreyCount) MaxPreyCount = _preyCount;
        if (_predatorCount > MaxPredatorCount) MaxPredatorCount = _predatorCount;
    }

    private void TickAnimal(Animal animal, float dt)
    {
        var state = animal.State;
        var perc = PerceptionSystem.Compute(animal, state, _grid, _cfg, _spatial, _plants, _settings, dt);
        var accel = SteeringSystem.Compute(state, perc, _settings, _grid, _cfg);

        animal.ApplySteering(accel, dt);
        MetabolismSystem.Tick(state, perc.currentSlope, _settings, dt);

        if (state.species == AnimalSpecies.Prey)
            animal.TryEatFruit(_plants, _settings);
        else
            TryAttack(animal, state);

        animal.TryDrink(_settings, dt);

        if (perc.mateFound && perc.mateCandidate != null)
            TryReproduce(animal, perc.mateCandidate);

        if (!state.IsAlive) _toRemove.Add(animal);
    }

    private void TryAttack(Animal predator, AnimalState pState)
    {
        if (pState.hunger < 0.2f) return;
        if (pState.attackCooldown > 0f) return;

        _spatial.Query(pState.position, _settings.attackRange, _queryBuf);

        foreach (var prey in _queryBuf)
        {
            if (prey == predator || !prey.IsAlive) continue;
            if (prey.State.species != AnimalSpecies.Prey) continue;
            if (Vector2.Distance(pState.position, prey.State.position) > _settings.attackRange) continue;

            Vector2 knockDir = (prey.State.position - pState.position).normalized;
            prey.State.velocity = knockDir * _settings.knockbackSpeed;

            MetabolismSystem.EatPrey(pState, _settings);
            pState.attackCooldown = _settings.attackCooldown;

            _toRemove.Add(prey);
            break;
        }
    }

    private void TryReproduce(Animal parentA, Animal parentB)
    {
        // Passa entrambi i genitori per impostare la parentela sulla prole
        var genes = ReproductionSystem.TryMate(parentA.State, parentB.State, _settings);
        if (genes == null) return;

        var state = ReproductionSystem.CreateOffspring(
            parentA.State, parentB.State, genes, _settings, _nextId++);

        var prefab = parentA.State.species == AnimalSpecies.Prey ? preyPrefab : predatorPrefab;
        if (prefab == null) return;

        var go = Instantiate(prefab,
            new Vector3(state.position.x, 0f, state.position.y),
            Quaternion.identity, animalContainer);
        var child = go.GetComponent<Animal>();
        if (child == null) { Destroy(go); return; }

        child.InitializeFromState(state, _grid, _cfg, _settings);
        _toAdd.Add(child);
    }

    private void SpawnAnimalFromEntry(SpawnEntry entry)
    {
        bool isPrey = entry.type == SpawnType.Prey;
        var species = isPrey ? AnimalSpecies.Prey : AnimalSpecies.Predator;
        GameObject prefab = isPrey ? preyPrefab : predatorPrefab;

        if (prefab == null) { Debug.LogWarning($"[SimulationRunner] Missing prefab for {species}"); return; }

        float h = _grid.SampleHeight(entry.worldX / _cfg.cellSize, entry.worldZ / _cfg.cellSize)
                  * _cfg.heightScale;
        var go = Instantiate(prefab,
            new Vector3(entry.worldX, h, entry.worldZ),
            Quaternion.identity, animalContainer);

        var animal = go.GetComponent<Animal>();
        if (animal == null)
        {
            Debug.LogError($"[SimulationRunner] Prefab {prefab.name} missing Animal component!");
            Destroy(go);
            return;
        }

        var genes = isPrey ? GeneticProfile.RandomForPrey() : GeneticProfile.RandomForPredator();
        animal.Initialize(species, entry.worldX, entry.worldZ, genes, _grid, _cfg, _settings, _nextId++);
        animal.State.reproductionCooldown = _settings.reproductionCooldown;
        // parentAId/parentBId rimangono -1: prima generazione senza storia

        AddAnimal(animal);
    }

    private void AddAnimal(Animal a)
    {
        _animals.Add(a);
        if (a.State.species == AnimalSpecies.Prey) _preyCount++;
        else _predatorCount++;
    }

    private void RemoveAnimal(Animal a)
    {
        if (a == null) return;
        _animals.Remove(a);
        if (a.State?.species == AnimalSpecies.Prey) _preyCount = Mathf.Max(0, _preyCount - 1);
        else _predatorCount = Mathf.Max(0, _predatorCount - 1);
        Destroy(a.gameObject);
    }

    public void StopSimulation()
    {
        foreach (var a in _animals) if (a) Destroy(a.gameObject);
        _animals.Clear();
        _toAdd.Clear();
        _toRemove.Clear();
        _preyCount = 0;
        _predatorCount = 0;
        _plants?.Clear();
        _grid = null;
        paused = true;
        ElapsedTime = 0f;
    }
}