using System.Collections.Generic;
using UnityEngine;

public class PlantManager : MonoBehaviour
{
    [Header("Prefab pianta")]
    public GameObject plantPrefab;

    [Header("Container")]
    public Transform plantContainer; // GO contenente tutte le piante

    private WorldGrid _grid;
    private SimulationSettings _settings;
    private RenderConfig _cfg;

    private PlantState[,] _plantStates;
    private readonly Dictionary<(int, int), GameObject> _plantObjects = new();
    private readonly Dictionary<(int, int), GameObject> _fruitIndicators = new();

    private float _growthAccumulator;
    private const float GROWTH_TICK_INTERVAL = 2f;

    private int _activePlantCount;
    private int _passableCellCount; // Numero di celle idonee per ospitare piante (no acqua, niente ostacoli, altezza maggiore di plantMinHeight)
    public int ActivePlantCount => _activePlantCount;

    public void Initialize(WorldGrid grid, SimulationSettings settings, RenderConfig cfg,
                           List<SpawnEntry> editorPlants)
    {
        _grid = grid; _settings = settings; _cfg = cfg;
        _activePlantCount = 0; _growthAccumulator = 0f;

        _passableCellCount = 0;
        for (int x = 0; x < grid.size; x++)
            for (int y = 0; y < grid.size; y++)
            {
                var c = grid.Get(x, y);
                if (!c.IsWater && !c.HasObstacle && c.height >= settings.plantMinHeight)
                    _passableCellCount++;
            }

        _plantStates = new PlantState[grid.size, grid.size];

        // Tra le SpawnEntry, prendiamo solo le piante e le spawniamo come permanenti
        foreach (var entry in editorPlants)
        {
            if (entry.type != SpawnType.Plant) continue;
            int cx = Mathf.RoundToInt(entry.worldX / cfg.cellSize);
            int cy = Mathf.RoundToInt(entry.worldZ / cfg.cellSize);
            if (!grid.IsInside(cx, cy)) continue;

            _plantStates[cx, cy] = new PlantState
            {
                hasPlant = true, hasFruit = true, fruitTimer = 0f,
                isPermanent = true, gridX = cx, gridY = cy,
            };
            _activePlantCount++;
            SpawnPlantGO(cx, cy, hasFruit: true);
        }
    }

    public void Tick(float dt)
    {
        // Aggiorniamo solo ogni GROWTH_TICK_INTERVAL secondi
        _growthAccumulator += dt;
        if (_growthAccumulator < GROWTH_TICK_INTERVAL) return;

        float tickDt = _growthAccumulator;
        _growthAccumulator = 0f;

        // Carrying capacity GLOBALE: la crescita rallenta man mano che la copertura si avvicina al tetto K (1 - n/K)
        float K = _settings.plantCarryingCapacityFraction * _passableCellCount;
        float globalFactor = Mathf.Max(0f, 1f - (_activePlantCount / Mathf.Max(1f, K)));

        // Esploriamo tutte le celle della mappa
        int n = _grid.size;
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                var cell = _grid.Get(x, y);
                // Escludiamo quelle non valide
                if (cell.IsWater || cell.HasObstacle) continue;
                if (cell.height < _settings.plantMinHeight) continue;

                var state = _plantStates[x, y];

                // Se la cella attuale non contiene una pianta, nasce con una certa probabilità
                if (state == null || !state.hasPlant)
                {
                    float spawnProb = (0.4f + 0.6f * cell.fertility) * _settings.plantGrowthRate * tickDt * globalFactor;
                    if (Random.value < spawnProb)
                        SpawnPlant(x, y, permanent: false);
                    continue;
                }

                // Se non ha il frutto, scorriamo il timer finché non è <= 0
                if (!state.hasFruit)
                {
                    float fertMult = 0.5f + cell.fertility;
                    state.fruitTimer -= tickDt * fertMult;
                    if (state.fruitTimer <= 0f)
                    {
                        state.hasFruit = true;
                        SetFruitIndicator(x, y, true);
                    }
                }
                // Se non è permanente, la pianta ha una probabilità di morire inversamente proporzionale alla fertilità della cella su cui poggia
                if (!state.isPermanent)
                {
                    float deathProb = (1f - cell.fertility) * 0.05f * tickDt;
                    if (Random.value < deathProb) KillPlant(x, y);
                }
            }
    }
    
    // Dice se una cella ha un frutto
    public bool HasFruit(int cx, int cy)
    {
        if (!_grid.IsInside(cx, cy)) return false;
        var s = _plantStates[cx, cy];
        return s != null && s.hasPlant && s.hasFruit;
    }

    // Chiamata in EcologySystem dalle prede, restituisce true se c'è un frutto, e lo toglie resettando il timer
    public bool TryEat(int cx, int cy, SimulationSettings settings)
    {
        if (!HasFruit(cx, cy)) return false;
        var state = _plantStates[cx, cy];
        state.hasFruit = false;
        state.fruitTimer = FruitRegrowTime(_grid.Get(cx, cy).fertility, settings);
        SetFruitIndicator(cx, cy, false);
        return true;
    }

    // Chiamato in PerceptionSystem dalle prede, per cercare cibo nei dintorni
    public void GetFruitCellsInRadius(Vector2 posXZ, float radius, List<Vector2Int> results)
    {
        results.Clear();
        int r = Mathf.CeilToInt(radius / _cfg.cellSize) + 1;
        int cx = Mathf.RoundToInt(posXZ.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(posXZ.y / _cfg.cellSize);
        float r2 = radius * radius;

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                if (!HasFruit(nx, ny)) continue;

                float wx = nx * _cfg.cellSize, wz = ny * _cfg.cellSize;
                float dxW = wx - posXZ.x, dzW = wz - posXZ.y;
                if (dxW * dxW + dzW * dzW <= r2)
                    results.Add(new Vector2Int(nx, ny));
            }
    }

    // Fa nascere una pianta
    private void SpawnPlant(int cx, int cy, bool permanent)
    {
        _plantStates[cx, cy] = new PlantState
        {
            hasPlant = true, hasFruit = false,
            fruitTimer = FruitRegrowTime(_grid.Get(cx, cy).fertility, _settings),
            isPermanent = permanent, gridX = cx, gridY = cy,
        };
        _activePlantCount++;
        SpawnPlantGO(cx, cy, hasFruit: false);
    }

    // Spawna il GO della pianta
    private void SpawnPlantGO(int cx, int cy, bool hasFruit)
    {
        if (plantPrefab == null || _plantObjects.ContainsKey((cx, cy))) return;

        float wx = cx * _cfg.cellSize, wz = cy * _cfg.cellSize;
        float wy = _grid.SampleHeight(cx, cy) * _cfg.heightScale;

        var go = Instantiate(plantPrefab, new Vector3(wx, wy, wz), Quaternion.identity, plantContainer);
        _plantObjects[(cx, cy)] = go;

        var indicator = go.transform.Find("FruitIndicator")?.gameObject; // FruitIndicator contiene il GO di quelle mele cinesi strane che ho usato come frutta
        if (indicator != null)
        {
            _fruitIndicators[(cx, cy)] = indicator;
            indicator.SetActive(hasFruit);
        }
    }

    // Despawna la pianta nella cella
    private void KillPlant(int cx, int cy)
    {
        _plantStates[cx, cy] = null;
        _activePlantCount = Mathf.Max(0, _activePlantCount - 1);

        if (_plantObjects.TryGetValue((cx, cy), out var go))
        {
            Destroy(go);
            _plantObjects.Remove((cx, cy));
        }
        _fruitIndicators.Remove((cx, cy));
    }

    // Toggle del fruit indicator nelle piante, segnale visivo che la pianta abbia i frutti
    // Non serve agli animali, solo all'utente per vedere la frutta apparire e scomparire
    private void SetFruitIndicator(int cx, int cy, bool active)
    {
        if (_fruitIndicators.TryGetValue((cx, cy), out var ind) && ind != null)
            ind.SetActive(active);
    }

    private static float FruitRegrowTime(float fertility, SimulationSettings s)
    {
        // Più fertile -> ricrescita più rapida. Banda 0.5x..1.5x del valore base.
        return s.fruitRegrowTime * Mathf.Lerp(0.5f, 1.5f, 1f - Mathf.Clamp01(fertility));
    }

    // Abbastanza intuitivo
    public void Clear()
    {
        foreach (var go in _plantObjects.Values) if (go) Destroy(go);
        _plantObjects.Clear();
        _fruitIndicators.Clear();
        _activePlantCount = 0;

        if (_plantStates != null)
        {
            int n = _plantStates.GetLength(0);
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                    _plantStates[x, y] = null;
        }
    }
}
