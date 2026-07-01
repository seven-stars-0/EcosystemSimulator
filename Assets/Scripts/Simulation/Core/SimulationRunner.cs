using System.Collections.Generic;
using UnityEngine;

// Orchestratore della simulazione in corso
public class SimulationRunner : MonoBehaviour
{
    public static SimulationRunner Instance { get; private set; }

    // Prefab di prede/predatori: UNICA fonte = AnimalSkinManager (con eventuali fallback)
    // Se il manager manca, il prefab e' null e lo spawn viene saltato.
    private GameObject PreyPrefabResolved
        => AnimalSkinManager.Instance != null ? AnimalSkinManager.Instance.PreyPrefab : null;
    private GameObject PredatorPrefabResolved
        => AnimalSkinManager.Instance != null ? AnimalSkinManager.Instance.PredatorPrefab : null;

    [Header("Container")]
    public Transform animalContainer; // GO che contiene gli animali come figli

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

    private readonly SimulationLogger _logger = new SimulationLogger(1f);  // 1 campionamento / s di default, si può cambiare in SimulationSettings (ParameterPanel)

    private int _nextId;

    // Statistiche
    public int MaxPreyCount { get; private set; }
    public int MaxPredatorCount { get; private set; }
    public int PreyCount => _preyCount;
    public int PredatorCount => _predatorCount;
    public int PlantCount => _plants?.ActivePlantCount ?? 0;
    public float ElapsedTime { get; private set; }

    // Usati da PopulationGraphHUD per comporre i grafici
    private int _preyCount;
    private int _predatorCount;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 
    public void StartSimulation(MapData data, PlantManager plants, bool logEnabled)
    {
        // Inizializziamo i valori della simulazione
        _grid = data.grid;
        _cfg = WorldSession.Instance.Renderer.config;
        _settings = data.simulationSettings;
        _plants = plants;
        _nextId = 0;
        ElapsedTime = 0f;
        _preyCount = 0; _predatorCount = 0;
        MaxPreyCount = 0; MaxPredatorCount = 0;

        _spatial = new SpatialGrid<Animal>(5f);

        // Le piante piazzate manualmente dall'utente vengono spawnate da PlantManager
        plants.Initialize(_grid, _settings, _cfg, data.spawnEntries);

        _animals.Clear();
        // Spawniamo tutti gli animali piazzati manualmente dall'utente con SpawnTool
        if (data.spawnEntries != null)
            foreach (var entry in data.spawnEntries)
                if (entry.type == SpawnType.Prey || entry.type == SpawnType.Predator)
                {
                    var sp = entry.type == SpawnType.Prey ? AnimalSpecies.Prey : AnimalSpecies.Predator;
                    SpawnAnimalAt(sp, entry.worldX, entry.worldZ);
                }

        // Spawniamo randomicamente gli animali indicati dall'utente 
        // Mathf.Max per essere sicuri di non usare valori negativi (anche se non è permesso farlo negli InputFields)
        SpawnRandom(AnimalSpecies.Prey, Mathf.Max(0, data.randomPreyCount));
        SpawnRandom(AnimalSpecies.Predator, Mathf.Max(0, data.randomPredatorCount));

        // Inizializziamo il logger se l'utente sceglie di tenere traccia dei cambiamenti
        if (logEnabled)
            _logger.Begin(_settings, _preyCount, _predatorCount, PlantCount);

        paused = false;
        Debug.Log($"[SimulationRunner] Start: {_preyCount} prey, {_predatorCount} pred.");
    }

    // Chiamato da SimulationSession (e indirettamente dalla UI)
    public void StopSimulation()
    {
        // Il logging termina
        _logger.End(MaxPreyCount, MaxPredatorCount, ElapsedTime);
        // I GO degli animali vengono distrutti
        foreach (var a in _animals) if (a) Destroy(a.gameObject);
        // Facciamo clear dei riferimenti agli animali
        _animals.Clear(); _toAdd.Clear(); _toRemove.Clear();
        _preyCount = 0; _predatorCount = 0;
        _plants?.Clear();
        _grid = null;
        paused = true;
        ElapsedTime = 0f;
    }

    // Fa il Tick di tutte le entità e dei sistemi involti nel funzionamento delle 
    private void Update()
    {
        // Se in pausa, non facciamo nulla
        if (paused || _grid == null) return;

        float dt = Time.deltaTime * timeScale;
        ElapsedTime += dt;

        // Rebuild di SpatialGrid
        _spatial.Clear();
        foreach (var a in _animals)
            if (a != null && a.IsAlive)
                _spatial.Insert(a.State.position, a);

        // Tick delle piante
        _plants.Tick(dt);

        // Tick degli animali
        foreach (var a in _animals)
        {
            if (a == null || !a.IsAlive) continue;
            TickAnimal(a, dt);
        }

        foreach (var n in _toAdd) AddAnimal(n);
        foreach (var d in _toRemove) RemoveAnimal(d);
        _toAdd.Clear();
        _toRemove.Clear();

        // Aggiorniamo le statistiche
        if (_preyCount > MaxPreyCount) MaxPreyCount = _preyCount;
        if (_predatorCount > MaxPredatorCount) MaxPredatorCount = _predatorCount;

        // Tick del log
        _logger.Tick(ElapsedTime, _preyCount, _predatorCount, PlantCount);
    }

    // Coordina i sistemi degli animali per determinare il comportamento
    // Dipende dalla velocità della simulazione (dt)
    private void TickAnimal(Animal animal, float dt)
    {
        // Movimento dell'animale
        var s = animal.State;
        var perc = PerceptionSystem.Compute(animal, s, _grid, _cfg, _spatial, _plants, _settings, dt);
        var accel = SteeringSystem.Compute(s, in perc, _settings, _grid, _cfg);
        animal.ApplySteering(accel, dt);

        // Metabolismo dell'animale
        EcologySystem.Metabolize(s, _settings, dt);

        bool reproduced;
        if (s.species == AnimalSpecies.Prey)
        {
            EcologySystem.Graze(s, _plants, _settings, _grid, _cfg);

            // FRENO LOGISTICO prede con TETTO DINAMICO:
            //   K_eff = K0 / (1 + sensibilita' * N_predatori)
            // Pochi predatori -> tetto alto (prede sbocciano)
            // molti predatori -> tetto basso (prede compresse)
            // Serve a generare le oscillazioni
            float K0 = _settings.preyCarryingCapacity;
            float Kprey = K0 / (1f + _settings.preyCapPredatorSensitivity * _predatorCount);
            float logistic = Mathf.Clamp01(1f - _preyCount / Mathf.Max(1f, Kprey));
            reproduced = Random.value < logistic && EcologySystem.TryReproduce(s, _settings);
        }
        else
        {
            var killed = EcologySystem.Hunt(animal, s, _spatial, _settings);
            if (killed != null) { killed.State.energy = 0f; _toRemove.Add(killed); }

            // CAP PREDATORI - prede (Leslie-Gower): natalita' * (1 - P/(r*N)).
            // La capacita' portante dei predatori scala col numero di prede:
            // smorza l'overshoot (predatori sterminano le prede) e impedisce
            // l'estinzione (con prede presenti il cap resta positivo).
            float predK = Mathf.Max(1f, _settings.predatorFoodRatio * _preyCount);
            float logisticPred = Mathf.Clamp01(1f - _predatorCount / predK);
            reproduced = Random.value < logisticPred && EcologySystem.TryReproduce(s, _settings);
        }

        if (reproduced) SpawnOffspring(animal);

        // Morte: energia/fame al limite (metabolismo base) OPPURE, solo per predatori, mortalita' da SCARSITA' di prede (dipende dal rapporto prede/predatori)
        // Per-individuo e stocastica -> niente coorti
        // sincronizzate (niente "spalle")
        // auto-limitante -> niente estinzione dei predatori finche' ci sono prede
        //
        // Questo serve perché, senza, i predatori sterminavano le prede riproducendosi a manetta nel processo
        // E' un modo artificiale per gestire la popolazione di predatori, ma funziona bene per fortuna
        bool scarcity = s.species == AnimalSpecies.Predator
                        && EcologySystem.ScarcityDeath(_preyCount, _predatorCount, _settings, dt);
        if (!s.IsAlive || scarcity)
            _toRemove.Add(animal);
    }

    // Crea prole tramite gemmazione
    private void SpawnOffspring(Animal parent)
    {
        var ps = parent.State;
        var prefab = ps.species == AnimalSpecies.Prey ? PreyPrefabResolved : PredatorPrefabResolved;
        if (prefab == null) return;

        // Posizione casuale di spawn della prole, entro un certo raggio dal genitore
        Vector2 cpos = ps.position + Random.insideUnitCircle * 0.6f;
        cpos.x = Mathf.Clamp(cpos.x, 0f, (_grid.size - 1) * _cfg.cellSize);
        cpos.y = Mathf.Clamp(cpos.y, 0f, (_grid.size - 1) * _cfg.cellSize);

        // Creazione del GO del figlio, con altezza corretta
        float h = _grid.SampleHeight(cpos.x / _cfg.cellSize, cpos.y / _cfg.cellSize) * _cfg.heightScale;
        var go = Instantiate(prefab, new Vector3(cpos.x, h, cpos.y), Quaternion.identity, animalContainer);
        go.transform.localScale *= (ps.species == AnimalSpecies.Predator ? _settings.predatorScale : _settings.preyScale);
        var child = go.GetComponent<Animal>();
        if (child == null) { Destroy(go); return; }

        // Neonato sotto soglia + cooldown di maturazione per evitare che si riproduca istantaneamente
        float childCooldown = ps.species == AnimalSpecies.Prey
            ? _settings.preyReproCooldown
            : _settings.predatorReproCooldown;

        // Creazione dell'AnimalState
        var st = new AnimalState
        {
            id = _nextId++,
            species = ps.species,
            genes = GeneticOps.Mutate(ps.genes, _settings), // Qui viene creato GeneticProfile con mutazioni
            energy = _settings.offspringEnergy,
            hunger = 0.3f,
            reproductionCooldown = childCooldown,
            position = cpos,
            velocity = Vector2.zero,
        };
        child.InitializeFromState(st);
        _toAdd.Add(child);
    }

    // Per spawnare gli animali piazzati a mano
    private void SpawnAnimalAt(AnimalSpecies species, float worldX, float worldZ)
    {
        GameObject prefab = species == AnimalSpecies.Prey ? PreyPrefabResolved : PredatorPrefabResolved;
        if (prefab == null) { Debug.LogWarning($"[SimulationRunner] Missing prefab for {species}"); return; }

        float h = _grid.SampleHeight(worldX / _cfg.cellSize, worldZ / _cfg.cellSize) * _cfg.heightScale;
        var go = Instantiate(prefab, new Vector3(worldX, h, worldZ), Quaternion.identity, animalContainer);
        go.transform.localScale *= (species == AnimalSpecies.Predator ? _settings.predatorScale : _settings.preyScale);
        var animal = go.GetComponent<Animal>();
        if (animal == null) { Debug.LogError($"[SimulationRunner] Prefab {prefab.name} missing Animal!"); Destroy(go); return; }

        var genes = species == AnimalSpecies.Prey ? GeneticProfile.RandomForPrey() : GeneticProfile.RandomForPredator();
        animal.Initialize(species, worldX, worldZ, genes, _nextId++);

        // Cooldown di riproduzione INIZIALE sfalsato
        // Serve perché, senza, la popolazione di entrambe le specie raddoppierebbe a t=0.
        float initCd = species == AnimalSpecies.Prey ? _settings.preyReproCooldown : _settings.predatorReproCooldown;
        animal.State.reproductionCooldown = Random.Range(0.5f, 1f) * initCd;

        AddAnimal(animal);
    }

    // Spawna in celle casuali adatte il numero di prede e predatori indicato dall'utente in SpawmTool
    private void SpawnRandom(AnimalSpecies species, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (TryFindSuitableCell(out int cx, out int cy))
                SpawnAnimalAt(species, cx * _cfg.cellSize, cy * _cfg.cellSize);
            else { Debug.LogWarning("[SimulationRunner] Nessuna cella adatta per lo spawn."); break; }
        }
    }

    // Una cella è adatta allo spawn se ha height > 0 e se non contiene ostacoli
    private bool TryFindSuitableCell(out int cx, out int cy)
    {
        int n = _grid.size;
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int x = Random.Range(0, n), y = Random.Range(0, n);
            var c = _grid.Get(x, y);
            if (!c.IsWater && !c.HasObstacle && c.height > 0f) { cx = x; cy = y; return true; }
        }
        cx = cy = 0; return false;
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
        if (!_animals.Remove(a)) return;
        if (a.State?.species == AnimalSpecies.Prey) _preyCount = Mathf.Max(0, _preyCount - 1);
        else _predatorCount = Mathf.Max(0, _predatorCount - 1);
        Destroy(a.gameObject);
    }
}
