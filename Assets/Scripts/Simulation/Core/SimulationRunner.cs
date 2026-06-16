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

    // ── Immigrazione ──────────────────────────────────────────────────────────
    private float _immigrationPreyTimer;
    private float _immigrationPredTimer;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Start / Stop ──────────────────────────────────────────────────────────

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

        // Immigrazione: parte già "pronta" così la prima immigrazione avviene
        // dopo il primo interval, non subito.
        _immigrationPreyTimer = _settings.immigrationInterval;
        _immigrationPredTimer = _settings.immigrationInterval;

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

    // ── Update loop ───────────────────────────────────────────────────────────

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

        // ── Micro-immigrazione ────────────────────────────────────────────────
        TickImmigration(dt);

        foreach (var n in _toAdd) AddAnimal(n);
        foreach (var d in _toRemove) RemoveAnimal(d);
        _toAdd.Clear();
        _toRemove.Clear();

        if (_preyCount > MaxPreyCount) MaxPreyCount = _preyCount;
        if (_predatorCount > MaxPredatorCount) MaxPredatorCount = _predatorCount;
    }

    // ── Tick singolo animale ──────────────────────────────────────────────────

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

    // ── TryAttack: predatore fallibile con Holling Type II ───────────────────

    private void TryAttack(Animal predator, AnimalState pState)
    {
        // Il predatore non caccia se ha poca fame o è in cooldown
        if (pState.hunger < 0.20f) return;
        if (pState.attackCooldown > 0f) return;

        _spatial.Query(pState.position, _settings.attackRange, _queryBuf);

        foreach (var prey in _queryBuf)
        {
            if (prey == predator || !prey.IsAlive) continue;
            if (prey.State.species != AnimalSpecies.Prey) continue;
            if (Vector2.Distance(pState.position, prey.State.position) > _settings.attackRange) continue;

            bool killSuccess = Random.value < _settings.killChance;

            if (killSuccess)
            {
                // ── KILL RIUSCITO ─────────────────────────────────────────────
                MetabolismSystem.Eat(pState, _settings);

                // Handling Time: il predatore "mangia" la preda e non caccia
                // per handlingTime secondi → risposta funzionale Holling II.
                pState.attackCooldown = _settings.handlingTime;

                _toRemove.Add(prey);
            }
            else
            {
                // ── ATTACCO FALLITO ───────────────────────────────────────────
                // La preda riceve knockback e si allontana velocemente.
                Vector2 knockDir = (prey.State.position - pState.position).normalized;
                prey.State.velocity = knockDir * _settings.knockbackSpeed;

                // Il predatore entra in mini-stun da sbilancio.
                pState.attackCooldown = _settings.missStunDuration;
            }

            // Un attacco per tick (riuscito o meno) → break
            break;
        }
    }

    // ── Micro-Immigrazione ────────────────────────────────────────────────────

    private void TickImmigration(float dt)
    {
        // ── Prede ─────────────────────────────────────────────────────────────
        if (_preyCount < _settings.immigrationThreshold)
        {
            _immigrationPreyTimer -= dt;
            if (_immigrationPreyTimer <= 0f)
            {
                _immigrationPreyTimer = _settings.immigrationInterval;
                SpawnImmigrant(AnimalSpecies.Prey);
                Debug.Log($"[Immigration] 1 preda immigrata ai bordi (pop={_preyCount})");
            }
        }
        else
        {
            // Reset timer: se la popolazione si riprende, il conteggio riparte
            _immigrationPreyTimer = _settings.immigrationInterval;
        }

        // ── Predatori ─────────────────────────────────────────────────────────
        if (_predatorCount < _settings.immigrationThreshold)
        {
            _immigrationPredTimer -= dt;
            if (_immigrationPredTimer <= 0f)
            {
                _immigrationPredTimer = _settings.immigrationInterval;
                SpawnImmigrant(AnimalSpecies.Predator);
                Debug.Log($"[Immigration] 1 predatore immigrato ai bordi (pop={_predatorCount})");
            }
        }
        else
        {
            _immigrationPredTimer = _settings.immigrationInterval;
        }
    }

    private void SpawnImmigrant(AnimalSpecies species)
    {
        // Posizione casuale su uno dei 4 bordi della mappa
        float maxW = (_grid.size - 1) * _cfg.cellSize;
        float margin = _cfg.cellSize * 2f;

        Vector2 pos = PickBorderPosition(maxW, margin);

        // Cerca una cella terrestre valida vicino al bordo scelto
        int cx = Mathf.RoundToInt(pos.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(pos.y / _cfg.cellSize);
        cx = Mathf.Clamp(cx, 0, _grid.size - 1);
        cy = Mathf.Clamp(cy, 0, _grid.size - 1);

        // Se la cella è acqua, cerca la più vicina passabile
        if (_grid.Get(cx, cy).IsWater || _grid.Get(cx, cy).HasObstacle)
        {
            bool found = false;
            for (int r = 1; r <= 5 && !found; r++)
                for (int dx = -r; dx <= r && !found; dx++)
                    for (int dy = -r; dy <= r && !found; dy++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (!_grid.IsInside(nx, ny)) continue;
                        if (_grid.Get(nx, ny).IsWater || _grid.Get(nx, ny).HasObstacle) continue;
                        cx = nx; cy = ny; found = true;
                    }
            if (!found) return;  // nessuna cella valida trovata
        }

        float worldX = cx * _cfg.cellSize;
        float worldZ = cy * _cfg.cellSize;

        bool isPrey = species == AnimalSpecies.Prey;
        GameObject prefab = isPrey ? preyPrefab : predatorPrefab;
        if (prefab == null) return;

        float h = _grid.SampleHeight(cx, cy) * _cfg.heightScale;
        var go = Instantiate(prefab,
            new Vector3(worldX, h, worldZ),
            Quaternion.identity, animalContainer);

        var animal = go.GetComponent<Animal>();
        if (animal == null) { Destroy(go); return; }

        var genes = isPrey ? GeneticProfile.RandomForPrey() : GeneticProfile.RandomForPredator();
        animal.Initialize(species, worldX, worldZ, genes, _grid, _cfg, _settings, _nextId++);

        // L'immigrante ha già il cooldown di riproduzione pieno (non si riproduce subito)
        animal.State.reproductionCooldown = _settings.reproductionCooldown;

        _toAdd.Add(animal);
    }

    private Vector2 PickBorderPosition(float maxW, float margin)
    {
        // 0=Top, 1=Bottom, 2=Left, 3=Right
        switch (Random.Range(0, 4))
        {
            case 0: return new Vector2(Random.Range(0f, maxW), margin);
            case 1: return new Vector2(Random.Range(0f, maxW), maxW - margin);
            case 2: return new Vector2(margin, Random.Range(0f, maxW));
            default: return new Vector2(maxW - margin, Random.Range(0f, maxW));
        }
    }

    // ── Reproduzione ─────────────────────────────────────────────────────────

    private void TryReproduce(Animal parentA, Animal parentB)
    {
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

    // ── Spawn da MapData ──────────────────────────────────────────────────────

    private void SpawnAnimalFromEntry(SpawnEntry entry)
    {
        bool isPrey = entry.type == SpawnType.Prey;
        var species = isPrey ? AnimalSpecies.Prey : AnimalSpecies.Predator;
        GameObject prefab = isPrey ? preyPrefab : predatorPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[SimulationRunner] Missing prefab for {species}");
            return;
        }

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

        AddAnimal(animal);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
}