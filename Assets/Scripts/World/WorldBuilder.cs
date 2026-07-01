using System.Collections.Generic;
using UnityEngine;

public class WorldBuilder : MonoBehaviour
{
    // Riferimenti presi da WorldSession (fonte unica): niente wiring manuale qui.
    private WorldRenderer worldRenderer => WorldSession.Instance.Renderer;
    private WorldEditor   worldEditor   => WorldSession.Instance.Editor;
    private WorldCamera   worldCamera   => WorldSession.Instance.Camera;

    [Header("Prefab piante")]
    public GameObject plantPrefab;

    [Header("Prefabs ostacoli")]
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;

    [Header("Containers")]
    public Transform obstacleContainer;
    public Transform entityContainer;   // editor entities (static prefabs)

    // Runtime tracking
    private readonly Dictionary<(int, int), GameObject> _obstacleObjects = new();
    private readonly Dictionary<SpawnEntry, GameObject>  _entityObjects   = new();

    // MapData attiva: presa da MapSession (fonte unica), non duplicata qui.
    private MapData Data => MapSession.Instance != null ? MapSession.Instance.CurrentMap : null;


    // Entry points
    public void BuildForEditor(MapData data)
    {
        Build(data, simulationMode: false);
        worldEditor.SetEnabled(true);
    }

    public void BuildForSimulation(MapData data)
    {
        Build(data, simulationMode: true);
        worldEditor.SetEnabled(false);
    }

    public void TearDown()
    {
        ClearAll();
        worldRenderer.Deinitialize();
    }

    // Build phases

    private void Build(MapData data, bool simulationMode)
    {
        if (data?.grid == null || data.metadata == null)
        {
            Debug.LogError("[WorldBuilder] Build failed: MapData/grid/metadata null.");
            return;
        }

        ClearAll();
        
        Phase1_Terrain(data);
        Phase2_Obstacles(data);
        
        // La terza fase è usata solo nell'editor
        // Lo spawn delle entità durante la simualzione è gestito da SimulationRunner e PlantManager
        if (!simulationMode) Phase3_EditorEntities(data);
        
        Phase4_Settings(data);
        Phase5_Camera(data);
    }

    private void Phase1_Terrain(MapData data)
    {
        // data.grid.EnsureSlopesUpToDate();
        worldRenderer.Initialize(data.grid);
    }

    // Scorriamo tutte le celle e piazziamo un prefab casuale tra quelli del tipo di ostacolo presente
    // Quindi ad ogni build del mondo potremmo trovare prefab diversi
    private void Phase2_Obstacles(MapData data)
    {
        for (int x = 0; x < data.grid.size; x++)
            for (int y = 0; y < data.grid.size; y++)
            {
                var cell = data.grid.Get(x, y);
                if (cell.obstacle != ObstacleType.None)
                    InstantiateObstacleGO(x, y, cell.obstacle);
            }
    }

    // SOLO NELL'EDITOR
    // Spawna le entità come prefab statici, senza il loro funzionamento in SimulationRunner
    private void Phase3_EditorEntities(MapData data)
    {
        if (data.spawnEntries == null) return;
        foreach (var entry in data.spawnEntries)
            InstantiateEntityGO(entry);
    }

    private void Phase4_Settings(MapData data)
    {
        data.simulationSettings ??= new SimulationSettings();
        data.spawnEntries       ??= new List<SpawnEntry>();
    }

    // Imposta i bound camera su (grid.size − 1) x cellSize, centra il pivot
    private void Phase5_Camera(MapData data)
    {
        float worldSize = (data.grid.size - 1) * worldRenderer.config.cellSize;
        worldCamera.SetWorldBounds(worldSize, worldSize);
        worldCamera.MovePivotTo(new Vector3(worldSize * 0.5f, 0f, worldSize * 0.5f));
    }

    // ── API per SpawnTool (editor) ────────────────────────────────────────────

    public GameObject PlaceObstacle(int cellX, int cellY, ObstacleType type)
    {
        if (Data == null || !Data.grid.IsInside(cellX, cellY)) return null;

        var cell = Data.grid.Get(cellX, cellY);
        if (cell.HasObstacle) return null;

        cell.obstacle = type;
        Data.metadata.isDirty = true;

        var go = InstantiateObstacleGO(cellX, cellY, type);
        worldRenderer.MarkDirty(DirtyFlags.Terrain | DirtyFlags.SpawnOverlay);
        return go;
    }

    public GameObject PlaceEntity(SpawnEntry entry)
    {
        if (Data == null) return null;

        Data.spawnEntries.Add(entry);
        Data.metadata.isDirty = true;

        return InstantiateEntityGO(entry);
    }

    public void EraseObstacle(int cellX, int cellY)
    {
        if (Data == null || !Data.grid.IsInside(cellX, cellY)) return;

        var cell = Data.grid.Get(cellX, cellY);
        if (cell.obstacle == ObstacleType.None) return;

        cell.obstacle = ObstacleType.None;
        Data.metadata.isDirty = true;

        var key = (cellX, cellY);
        if (_obstacleObjects.TryGetValue(key, out var go)) { Destroy(go); _obstacleObjects.Remove(key); }

        worldRenderer.MarkDirty(DirtyFlags.Terrain | DirtyFlags.SpawnOverlay);
    }

    public void EraseEntity(float hitX, float hitZ)
    {
        if (Data?.spawnEntries == null) return;

        float tolerance = worldRenderer.config.cellSize * 0.6f;
        SpawnEntry best = null; float bestDist = float.MaxValue;

        // Tra tutte le SpawnEntry, cerchiamo quella più vicina al click del mouse, entro una certa distanza 'tolerance'
        foreach (var entry in Data.spawnEntries)
        {
            float dx = entry.worldX - hitX, dz = entry.worldZ - hitZ;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < bestDist && dist < tolerance) { bestDist = dist; best = entry; }
        }

        if (best == null) return;

        Data.spawnEntries.Remove(best);
        Data.metadata.isDirty = true;

        if (_entityObjects.TryGetValue(best, out var go)) { Destroy(go); _entityObjects.Remove(best); }
    }

    // ── API per TerrainTool ───────────────────────────────────────────────────

    public void SyncEntitiesHeight()
    {
        if (Data == null) return;
        var cfg = worldRenderer.config; // Serve per heightScale
        var obstaclesToRemove = new List<(int, int)>();

        foreach (var kv in _obstacleObjects)
        {
            (int cx, int cy) = kv.Key;

            if (!Data.grid.IsInside(cx, cy)) continue;

            float h = Data.grid.Get(cx, cy).height;

            // Se con il TerrainTool abbiamo abbassato l'altezza di una cella a <= 0, mentre un ostacolo era sopra di essa,
            // questo viene despawnata
            if (h <= 0f)
            {
                Destroy(kv.Value);
                Data.grid.Get(cx, cy).obstacle = ObstacleType.None;
                obstaclesToRemove.Add(kv.Key);
            }
            // Altrimenti aggiorniamo la posizione per adattarla alla nuova altezza
            else
            {
                kv.Value.transform.position = ComputeObstaclePosition(cx, cy);
            }
        }
        foreach (var key in obstaclesToRemove) _obstacleObjects.Remove(key);

        var entitiesToRemove = new List<SpawnEntry>();
        // Qui è lo stesso discorso degli ostacoli
        // L'unica differenza è che le entità usano l'interpolazione bilineare (SampleHeight) per calcolare la nuova altezza,
        // poiché non sono necessariamente al centro della loro cella
        foreach (var kv in _entityObjects)
        {
            var entry = kv.Key;
            float wx = entry.worldX / cfg.cellSize, wz = entry.worldZ / cfg.cellSize;
            float h  = Data.grid.SampleHeight(wx, wz);
            if (h <= 0f)
            {
                Destroy(kv.Value);
                entitiesToRemove.Add(entry);
            }
            else
            {
                kv.Value.transform.position = ComputeEntityPosition(entry.worldX, entry.worldZ);
            }
        }
        foreach (var entry in entitiesToRemove)
        {
            Data.spawnEntries.Remove(entry);
            _entityObjects.Remove(entry);
        }

        if (obstaclesToRemove.Count > 0 || entitiesToRemove.Count > 0)
            Data.metadata.isDirty = true;
    }

    // ── Instantiation helpers ─────────────────────────────────────────────────

    private GameObject InstantiateObstacleGO(int cx, int cy, ObstacleType type)
    {
        var prefab = PickObstaclePrefab(type); // Scelta casuale
        if (prefab == null) { Debug.LogWarning($"[WorldBuilder] Missing prefab for {type}"); return null; }

        var pos = ComputeObstaclePosition(cx, cy);
        var go  = Instantiate(prefab, pos, Quaternion.identity, obstacleContainer);
        float cs = worldRenderer.config.cellSize;

        go.transform.localScale = new Vector3(cs, go.transform.localScale.y, cs);
        _obstacleObjects[(cx, cy)] = go;
        return go;
    }

    private GameObject InstantiateEntityGO(SpawnEntry entry)
    {
        var prefab = GetEntityPrefab(entry.type);
        if (prefab == null) { Debug.LogWarning($"[WorldBuilder] Missing prefab for {entry.type}"); return null; }

        var pos = ComputeEntityPosition(entry.worldX, entry.worldZ);
        var go  = Instantiate(prefab, pos, Quaternion.identity, entityContainer);
        go.transform.localScale *= EntityScale(entry.type);
        _entityObjects[entry] = go;
        return go;
    }

    private Vector3 ComputeObstaclePosition(int cx, int cy)
    {
        var cfg = worldRenderer.config;
        return new Vector3(cx * cfg.cellSize,
                           Data.grid.SampleHeight(cx, cy) * cfg.heightScale,
                           cy * cfg.cellSize);
    }

    private Vector3 ComputeEntityPosition(float worldX, float worldZ)
    {
        var   cfg = worldRenderer.config;
        float h   = Data.grid.SampleHeight(worldX / cfg.cellSize, worldZ / cfg.cellSize);
        return new Vector3(worldX, h * cfg.heightScale, worldZ);
    }

    // Sceglie un prefab casuale tra quelli disponibili per il tipo di ostacolo selezionato
    private GameObject PickObstaclePrefab(ObstacleType type)
    {
        var arr = type == ObstacleType.Tree ? treePrefabs : rockPrefabs;
        if (arr == null || arr.Length == 0) return null;
        return arr[Random.Range(0, arr.Length)];
    }

    // Scala per specie (prede/predatori) dalle settings; 1 per il resto.
    private float EntityScale(SpawnType type)
    {
        var sett = Data?.simulationSettings;
        if (sett == null) return 1f;
        if (type == SpawnType.Predator) return sett.predatorScale;
        if (type == SpawnType.Prey)     return sett.preyScale;
        return 1f;
    }

    private GameObject GetEntityPrefab(SpawnType type) => type switch
    {
        SpawnType.Prey      => AnimalSkinManager.Instance != null ? AnimalSkinManager.Instance.PreyPrefab     : null,
        SpawnType.Predator  => AnimalSkinManager.Instance != null ? AnimalSkinManager.Instance.PredatorPrefab : null,
        SpawnType.Plant     => plantPrefab,
        _                   => null
    };

    // Uccide tutte le entità

    private void ClearAll()
    {
        foreach (var go in _obstacleObjects.Values) if (go) Destroy(go);
        _obstacleObjects.Clear();
        foreach (var go in _entityObjects.Values) if (go) Destroy(go);
        _entityObjects.Clear();
    }

    private void OnDestroy() => ClearAll();
}
