using System.Collections.Generic;
using UnityEngine;

public class WorldBuilder : MonoBehaviour
{
    [Header("References")]
    public WorldRenderer worldRenderer;
    public WorldEditor   worldEditor;
    public WorldCamera   worldCamera;

    [Header("Prefabs entità (editor)")]
    public GameObject preyPrefab;
    public GameObject predatorPrefab;
    public GameObject plantPrefab;

    [Header("Prefabs ostacoli")]
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;

    [Header("Containers")]
    public Transform obstacleContainer;
    public Transform entityContainer;   // editor entities (static prefabs)

    // ── Runtime tracking ──────────────────────────────────────────────────────

    private readonly Dictionary<(int, int), GameObject> _obstacleObjects = new();
    private readonly Dictionary<SpawnEntry, GameObject>  _entityObjects   = new();

    private MapData _data;
    private bool    _isSimulationMode;

    // ── Entry points ──────────────────────────────────────────────────────────

    public void BuildForEditor(MapData data)
    {
        Build(data, simulationMode: false);
        worldEditor.SetEnabled(true);
    }

    public void BuildForSimulation(MapData data)
    {
        // In simulation mode we do NOT place animals or editor-plants:
        // - Animals  → SimulationRunner.StartSimulation spawns them into animalContainer
        // - Plants   → PlantManager.Initialize manages them with full lifecycle
        // Only terrain + obstacles are built here.
        Build(data, simulationMode: true);
        worldEditor.SetEnabled(false);
    }

    public void TearDown()
    {
        ClearAll();
        _data = null;
        worldRenderer.Deinitialize();
    }

    // ── Build phases ──────────────────────────────────────────────────────────

    private void Build(MapData data, bool simulationMode)
    {
        ClearAll();
        _data              = data;
        _isSimulationMode  = simulationMode;

        Phase1_Terrain(data);
        Phase2_Obstacles(data);
        Phase3_EditorEntities(data, simulationMode);
        Phase4_Settings(data);
        Phase5_Camera(data);
    }

    private void Phase1_Terrain(MapData data)
    {
        data.grid.EnsureSlopesUpToDate();
        worldRenderer.Initialize(data.grid);
    }

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

    /// <summary>
    /// In editor mode: places all spawn entries as static prefabs.
    /// In simulation mode: skipped entirely — SimulationRunner and PlantManager
    ///   own animal and plant lifecycle respectively.
    /// </summary>
    private void Phase3_EditorEntities(MapData data, bool simulationMode)
    {
        if (simulationMode) return;   // ← KEY: nothing to do in simulation

        if (data.spawnEntries == null) return;
        foreach (var entry in data.spawnEntries)
            InstantiateEntityGO(entry);
    }

    private void Phase4_Settings(MapData data)
    {
        data.simulationSettings ??= new SimulationSettings();
    }

    private void Phase5_Camera(MapData data)
    {
        float worldSize = (data.metadata.gridSize - 1) * worldRenderer.config.cellSize;
        worldCamera.SetWorldBounds(worldSize, worldSize);
        worldCamera.MovePivotTo(new Vector3(worldSize * 0.5f, 0f, worldSize * 0.5f));
        worldEditor.grid     = data.grid;
        worldEditor.renderer = worldRenderer;
    }

    // ── API per SpawnTool (editor) ────────────────────────────────────────────

    public GameObject PlaceObstacle(int cellX, int cellY, ObstacleType type)
    {
        if (_data == null || !_data.grid.IsInside(cellX, cellY)) return null;
        var cell = _data.grid.Get(cellX, cellY);
        if (cell.HasObstacle) return null;
        cell.obstacle          = type;
        _data.metadata.isDirty = true;
        var go = InstantiateObstacleGO(cellX, cellY, type);
        worldRenderer.MarkDirty(DirtyFlags.Terrain | DirtyFlags.SpawnOverlay);
        return go;
    }

    public GameObject PlaceEntity(SpawnEntry entry)
    {
        if (_data == null) return null;
        _data.spawnEntries.Add(entry);
        _data.metadata.isDirty = true;
        return InstantiateEntityGO(entry);
    }

    public void EraseObstacle(int cellX, int cellY)
    {
        if (_data == null || !_data.grid.IsInside(cellX, cellY)) return;
        var cell = _data.grid.Get(cellX, cellY);
        if (cell.obstacle == ObstacleType.None) return;
        cell.obstacle          = ObstacleType.None;
        _data.metadata.isDirty = true;
        var key = (cellX, cellY);
        if (_obstacleObjects.TryGetValue(key, out var go)) { Destroy(go); _obstacleObjects.Remove(key); }
        worldRenderer.MarkDirty(DirtyFlags.Terrain | DirtyFlags.SpawnOverlay);
    }

    public void EraseEntity(float hitX, float hitZ)
    {
        if (_data?.spawnEntries == null) return;
        float tolerance = worldRenderer.config.cellSize * 0.6f;
        SpawnEntry best = null; float bestDist = float.MaxValue;
        foreach (var entry in _data.spawnEntries)
        {
            float dx = entry.worldX - hitX, dz = entry.worldZ - hitZ;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < bestDist && dist < tolerance) { bestDist = dist; best = entry; }
        }
        if (best == null) return;
        _data.spawnEntries.Remove(best);
        _data.metadata.isDirty = true;
        if (_entityObjects.TryGetValue(best, out var go)) { Destroy(go); _entityObjects.Remove(best); }
    }

    // ── API per TerrainTool ───────────────────────────────────────────────────

    public void SyncEntitiesHeight()
    {
        var cfg = worldRenderer.config;
        var obstaclesToRemove = new List<(int, int)>();

        foreach (var kv in _obstacleObjects)
        {
            (int cx, int cy) = kv.Key;
            if (!_data.grid.IsInside(cx, cy)) continue;
            float h = _data.grid.Get(cx, cy).height;
            if (h <= 0f)
            {
                Destroy(kv.Value);
                _data.grid.Get(cx, cy).obstacle = ObstacleType.None;
                obstaclesToRemove.Add(kv.Key);
            }
            else
            {
                var pos = kv.Value.transform.position;
                kv.Value.transform.position = new Vector3(pos.x, h * cfg.heightScale, pos.z);
            }
        }
        foreach (var key in obstaclesToRemove) _obstacleObjects.Remove(key);

        var entitiesToRemove = new List<SpawnEntry>();
        foreach (var kv in _entityObjects)
        {
            var entry = kv.Key;
            float wx = entry.worldX / cfg.cellSize, wz = entry.worldZ / cfg.cellSize;
            float h  = _data.grid.SampleHeight(wx, wz);
            if (h <= 0f)
            {
                Destroy(kv.Value);
                entitiesToRemove.Add(entry);
            }
            else
            {
                var pos = kv.Value.transform.position;
                kv.Value.transform.position = new Vector3(pos.x, h * cfg.heightScale, pos.z);
            }
        }
        foreach (var entry in entitiesToRemove)
        {
            _data.spawnEntries.Remove(entry);
            _entityObjects.Remove(entry);
        }

        if (obstaclesToRemove.Count > 0 || entitiesToRemove.Count > 0)
            _data.metadata.isDirty = true;
    }

    // ── Instantiation helpers ─────────────────────────────────────────────────

    private GameObject InstantiateObstacleGO(int cx, int cy, ObstacleType type)
    {
        var prefab = PickObstaclePrefab(type);
        if (prefab == null) { Debug.LogWarning($"[WorldBuilder] Prefab mancante per {type}"); return null; }
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
        if (prefab == null) { Debug.LogWarning($"[WorldBuilder] Nessun prefab per {entry.type}"); return null; }

        var pos = ComputeEntityPosition(entry.worldX, entry.worldZ);
        var go  = Instantiate(prefab, pos, Quaternion.identity, entityContainer);
        _entityObjects[entry] = go;
        return go;
    }

    private Vector3 ComputeObstaclePosition(int cx, int cy)
    {
        var cfg = worldRenderer.config;
        return new Vector3(cx * cfg.cellSize,
                           _data.grid.SampleHeight(cx, cy) * cfg.heightScale,
                           cy * cfg.cellSize);
    }

    private Vector3 ComputeEntityPosition(float worldX, float worldZ)
    {
        var   cfg = worldRenderer.config;
        float h   = _data.grid.SampleHeight(worldX / cfg.cellSize, worldZ / cfg.cellSize);
        return new Vector3(worldX, h * cfg.heightScale, worldZ);
    }

    private GameObject PickObstaclePrefab(ObstacleType type)
    {
        var arr = type == ObstacleType.Tree ? treePrefabs : rockPrefabs;
        if (arr == null || arr.Length == 0) return null;
        return arr[Random.Range(0, arr.Length)];
    }

    private GameObject GetEntityPrefab(SpawnType type) => type switch
    {
        SpawnType.Prey      => preyPrefab,
        SpawnType.Predator  => predatorPrefab,
        SpawnType.Plant     => plantPrefab,
        _                   => null
    };

    private void ClearAll()
    {
        foreach (var go in _obstacleObjects.Values) if (go) Destroy(go);
        _obstacleObjects.Clear();
        foreach (var go in _entityObjects.Values) if (go) Destroy(go);
        _entityObjects.Clear();
    }

    private void OnDestroy() => ClearAll();
}