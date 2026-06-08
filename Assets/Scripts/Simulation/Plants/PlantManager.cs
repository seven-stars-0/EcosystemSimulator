using System.Collections.Generic;
using UnityEngine;

public class PlantManager : MonoBehaviour
{
    [Header("Prefab pianta per quelle procedurali")]
    public GameObject plantPrefab;

    [Header("Container")]
    public Transform plantContainer;

    // ── State ─────────────────────────────────────────────────────────────────

    private WorldGrid          _grid;
    private SimulationSettings _settings;
    private RenderConfig       _cfg;

    private PlantState[,] _plantStates;

    // GO per le piante istanziate
    private readonly Dictionary<(int, int), GameObject> _plantObjects    = new();
    // Riferimenti ai GO figli "FruitIndicator" per ogni pianta
    private readonly Dictionary<(int, int), GameObject> _fruitIndicators = new();

    private float _growthAccumulator;
    private const float GROWTH_TICK_INTERVAL = 2f;

    // ── Contatore O(1) ────────────────────────────────────────────────────────
    private int _activePlantCount;
    public  int ActivePlantCount => _activePlantCount;

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Initialize(WorldGrid grid, SimulationSettings settings, RenderConfig cfg,
                           List<SpawnEntry> editorPlants)
    {
        _grid     = grid;
        _settings = settings;
        _cfg      = cfg;
        _activePlantCount = 0;
        _growthAccumulator = 0f;

        _plantStates = new PlantState[grid.size, grid.size];

        // Piante permanenti (piazzate dall'editor)
        foreach (var entry in editorPlants)
        {
            if (entry.type != SpawnType.Plant) continue;

            int cx = Mathf.RoundToInt(entry.worldX / cfg.cellSize);
            int cy = Mathf.RoundToInt(entry.worldZ / cfg.cellSize);

            if (!grid.IsInside(cx, cy)) continue;

            var state = new PlantState
            {
                hasPlant    = true,
                hasFruit    = true,
                fruitTimer  = 0f,
                isPermanent = true,
                gridX       = cx,
                gridY       = cy,
            };
            _plantStates[cx, cy] = state;
            _activePlantCount++;

            // Istanzia GO per le piante permanenti (gestione visuale in simulazione)
            SpawnPlantGO(cx, cy, hasFruit: true);
        }
    }

    // ── Tick ──────────────────────────────────────────────────────────────────

    public void Tick(float dt)
    {
        _growthAccumulator += dt;
        if (_growthAccumulator < GROWTH_TICK_INTERVAL) return;

        float tickDt = _growthAccumulator;
        _growthAccumulator = 0f;

        int n = _grid.size;

        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                var cell = _grid.Get(x, y);

                // ── Condizioni per la crescita ────────────────────────────────
                // Deve essere terra (non acqua), non ostruita, sopra la soglia minima
                if (cell.IsWater || cell.HasObstacle) continue;
                if (cell.height < _settings.plantMinHeight) continue;   // NO acqua/sabbia

                var state = _plantStates[x, y];

                if (state == null || !state.hasPlant)
                {
                    float spawnProb = cell.fertility * _settings.plantGrowthRate * tickDt;
                    if (Random.value < spawnProb)
                        SpawnPlant(x, y, permanent: false);
                }
                else
                {
                    // Frutti
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

                    // Morte naturale (solo piante non permanenti)
                    if (!state.isPermanent)
                    {
                        float deathProb = (1f - cell.fertility) * 0.05f * tickDt;
                        if (Random.value < deathProb)
                            KillPlant(x, y);
                    }
                }
            }
    }

    // ── API pubblica ──────────────────────────────────────────────────────────

    public bool HasFruit(int cx, int cy)
    {
        if (!_grid.IsInside(cx, cy)) return false;
        var s = _plantStates[cx, cy];
        return s != null && s.hasPlant && s.hasFruit;
    }

    public bool TryEat(int cx, int cy, SimulationSettings settings)
    {
        if (!HasFruit(cx, cy)) return false;
        var state      = _plantStates[cx, cy];
        state.hasFruit = false;
        state.fruitTimer = FruitRegrowTime(_grid.Get(cx, cy).fertility, settings);
        SetFruitIndicator(cx, cy, false);   // nasconde il pallino rosso
        return true;
    }

    public void GetFruitCellsInRadius(Vector2 posXZ, float radius, List<Vector2Int> results)
    {
        results.Clear();
        int r  = Mathf.CeilToInt(radius / _cfg.cellSize) + 1;
        int cx = Mathf.RoundToInt(posXZ.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(posXZ.y / _cfg.cellSize);
        float r2 = radius * radius;

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                if (!HasFruit(nx, ny)) continue;

                float wx  = nx * _cfg.cellSize;
                float wz  = ny * _cfg.cellSize;
                float dxW = wx - posXZ.x, dzW = wz - posXZ.y;
                if (dxW * dxW + dzW * dzW <= r2)
                    results.Add(new Vector2Int(nx, ny));
            }
    }

    // ── Spawn / Kill ──────────────────────────────────────────────────────────

    private void SpawnPlant(int cx, int cy, bool permanent)
    {
        var state = new PlantState
        {
            hasPlant    = true,
            hasFruit    = false,
            fruitTimer  = FruitRegrowTime(_grid.Get(cx, cy).fertility, _settings),
            isPermanent = permanent,
            gridX       = cx,
            gridY       = cy,
        };
        _plantStates[cx, cy] = state;
        _activePlantCount++;

        SpawnPlantGO(cx, cy, hasFruit: false);
    }

    /// <summary>
    /// Istanzia il GO visivo della pianta e cerca il child "FruitIndicator".
    /// Il prefab deve avere un figlio chiamato esattamente "FruitIndicator".
    /// </summary>
    private void SpawnPlantGO(int cx, int cy, bool hasFruit)
    {
        if (plantPrefab == null || _plantObjects.ContainsKey((cx, cy))) return;

        float wx = cx * _cfg.cellSize;
        float wz = cy * _cfg.cellSize;
        float wy = _grid.SampleHeight(cx, cy) * _cfg.heightScale;

        var go = Instantiate(plantPrefab,
            new Vector3(wx, wy, wz), Quaternion.identity, plantContainer);
        _plantObjects[(cx, cy)] = go;

        // Cerca il child FruitIndicator (pallino rosso)
        var indicator = go.transform.Find("FruitIndicator")?.gameObject;
        if (indicator != null)
        {
            _fruitIndicators[(cx, cy)] = indicator;
            indicator.SetActive(hasFruit);
        }
    }

    private void KillPlant(int cx, int cy)
    {
        _plantStates[cx, cy] = null;
        _activePlantCount    = Mathf.Max(0, _activePlantCount - 1);

        if (_plantObjects.TryGetValue((cx, cy), out var go))
        {
            Destroy(go);
            _plantObjects.Remove((cx, cy));
        }
        _fruitIndicators.Remove((cx, cy));
    }

    private void SetFruitIndicator(int cx, int cy, bool active)
    {
        if (_fruitIndicators.TryGetValue((cx, cy), out var ind) && ind != null)
            ind.SetActive(active);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float FruitRegrowTime(float fertility, SimulationSettings s)
    {
        float t = 1f - Mathf.Clamp01(fertility);
        return Mathf.Lerp(s.fruitRegrowTimeMin, s.fruitRegrowTimeMax, t);
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    public void Clear()
    {
        foreach (var go in _plantObjects.Values)
            if (go) Destroy(go);
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