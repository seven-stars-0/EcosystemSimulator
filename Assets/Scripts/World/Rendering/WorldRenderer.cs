using System;
using UnityEngine;

[Flags]
public enum DirtyFlags
{
    None = 0,
    Terrain = 1 << 0,
    SpawnOverlay = 1 << 1,
    All = ~0
}

public class WorldRenderer : MonoBehaviour
{
    [Header("Config")]
    public RenderConfig config = new RenderConfig();

    [Header("Views")]
    public TerrainView terrainView;
    public SpawnView spawnOverlayView;

    private WorldGrid _grid;
    private DirtyFlags _dirty = DirtyFlags.None;

    public void Initialize(WorldGrid grid)
    {
        _grid = grid;
        if (terrainView == null || spawnOverlayView == null)
        {
            Debug.LogError("[WorldRenderer] View non collegate nell'Inspector (terrainView/spawnView).");
            return;
        }
        terrainView.SetVisible(true);
        spawnOverlayView.SetVisible(false);
        terrainView.Build(_grid, config);
        spawnOverlayView.Build(_grid, config);
    }

    /// <summary>
    /// Resetta il renderer: nasconde le view e rilascia il riferimento alla griglia.
    /// Chiamato da WorldBuilder.TearDown().
    /// </summary>
    public void Deinitialize()
    {
        terrainView.SetVisible(false);
        spawnOverlayView.SetVisible(false);
        _grid = null;
        _dirty = DirtyFlags.None;
    }

    public void MarkDirty(DirtyFlags flags = DirtyFlags.All) => _dirty |= flags;
    public void SetSpawnOverlayVisible(bool v) => spawnOverlayView.SetVisible(v);

    private void LateUpdate()
    {
        if (_grid == null || _dirty == DirtyFlags.None) return;

        if (_dirty.HasFlag(DirtyFlags.Terrain))
            terrainView.Refresh(_grid, config);

        if (_dirty.HasFlag(DirtyFlags.SpawnOverlay) && spawnOverlayView.gameObject.activeSelf)
            spawnOverlayView.Refresh(_grid, config);

        _dirty = DirtyFlags.None;
    }
}