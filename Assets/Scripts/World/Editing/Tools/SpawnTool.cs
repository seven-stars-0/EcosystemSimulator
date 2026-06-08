using UnityEngine;

public class SpawnTool : IEditorTool
{
    public string ToolName => "Spawn";

    public SpawnableType CurrentSpawnable { get; set; } = SpawnableType.Prey;
    public bool          IsErasing        { get; set; } = false;

    // Solo il dato puro — niente riferimenti a sistemi Unity
    private readonly MapData _mapData;

    public SpawnTool(MapData mapData)
    {
        _mapData = mapData;
    }

    // ── Ciclo vita ────────────────────────────────────────────────────────────

    public void OnActivate()
    {
        WorldSession.Instance.Renderer.MarkDirty(DirtyFlags.SpawnOverlay);
        WorldSession.Instance.Renderer.SetSpawnOverlayVisible(true);
    }

    public void OnDeactivate()
    {
        WorldSession.Instance.Renderer.SetSpawnOverlayVisible(false);
    }

    public void OnClick(CellHit hit)     => Execute(hit);
    public void OnDragStart(CellHit hit) => Execute(hit);
    public void OnDrag(CellHit hit)      { }
    public void OnDragEnd(CellHit hit)   { }

    // ── Logica ────────────────────────────────────────────────────────────────

    private void Execute(CellHit hit)
    {
        if (IsErasing) TryErase(hit);
        else           TryPlace(hit);
    }

    private void TryPlace(CellHit hit)
    {
        if (!IsCellValidForSpawn(hit.cell)) return;

        var builder = WorldSession.Instance.Builder;

        switch (CurrentSpawnable)
        {
            case SpawnableType.Tree:
                builder.PlaceObstacle(hit.x, hit.y, ObstacleType.Tree);
                break;
            case SpawnableType.Rock:
                builder.PlaceObstacle(hit.x, hit.y, ObstacleType.Rock);
                break;
            default:
                PlaceEntity(hit, builder);
                break;
        }
    }

    private void PlaceEntity(CellHit hit, WorldBuilder builder)
    {
        float cs = WorldSession.Instance.Renderer.config.cellSize;
        float wx = hit.worldPosition.x;
        float wz = hit.worldPosition.z;

        bool occupied = _mapData.spawnEntries.Exists(e =>
        {
            float dx = e.worldX - wx, dz = e.worldZ - wz;
            return Mathf.Sqrt(dx * dx + dz * dz) < cs * 0.5f;
        });
        if (occupied) return;

        builder.PlaceEntity(new SpawnEntry
        {
            type   = ToSpawnType(CurrentSpawnable),
            worldX = wx,
            worldZ = wz,
        });
        MapSession.Instance.MarkDirty();
    }

    private void TryErase(CellHit hit)
    {
        var builder = WorldSession.Instance.Builder;
        if (hit.cell.HasObstacle)
            builder.EraseObstacle(hit.x, hit.y);
        else
            builder.EraseEntity(hit.worldPosition.x, hit.worldPosition.z);
        MapSession.Instance.MarkDirty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public static bool IsCellValidForSpawn(CellData cell)
        => cell.height > 0f && !cell.HasObstacle;

    private static SpawnType ToSpawnType(SpawnableType t) => t switch
    {
        SpawnableType.Prey     => SpawnType.Prey,
        SpawnableType.Predator => SpawnType.Predator,
        SpawnableType.Plant    => SpawnType.Plant,
        _                      => SpawnType.Plant,
    };
}