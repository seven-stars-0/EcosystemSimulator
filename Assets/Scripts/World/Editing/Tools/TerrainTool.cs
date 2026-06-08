using UnityEngine;

public class TerrainTool : IEditorTool
{
    public string ToolName => "Terrain";

    public float Strength = 0.2f;
    public int   Radius   = 3;
    public bool  Raise    = true;
    public bool  Smooth   = false;

    private readonly WorldGrid _grid;

    public TerrainTool(WorldGrid grid)
    {
        _grid = grid;
    }

    public void OnActivate()   { }
    public void OnDeactivate() { }

    public void OnClick(CellHit hit)     => Apply(hit.x, hit.y);
    public void OnDragStart(CellHit hit) => Apply(hit.x, hit.y);
    public void OnDrag(CellHit hit)      => Apply(hit.x, hit.y);

    public void OnDragEnd(CellHit hit)
    {
        _grid.RecalculateGradients();
        WorldSession.Instance.Renderer.MarkDirty(DirtyFlags.SpawnOverlay);
    }

    private void Apply(int cx, int cy)
    {
        if (Smooth) ApplySmooth(cx, cy);
        else        ApplyRaiseLower(cx, cy);

        _grid.InvalidateSlopes();
        MapSession.Instance?.MarkDirty();

        WorldSession.Instance.Renderer.MarkDirty(DirtyFlags.Terrain);
        WorldSession.Instance.Builder.SyncEntitiesHeight();
    }

    private void ApplyRaiseLower(int cx, int cy)
    {
        float delta = Strength * (Raise ? 1f : -1f) * Time.deltaTime;
        for (int dx = -Radius; dx <= Radius; dx++)
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                float dist    = Mathf.Sqrt(dx * dx + dy * dy);
                float falloff = Gaussian(dist, Radius);
                _grid.Get(nx, ny).height += delta * falloff;
            }
    }

    private void ApplySmooth(int cx, int cy)
    {
        for (int dx = -Radius; dx <= Radius; dx++)
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > Radius) continue;
                float avg     = SampleAverage(nx, ny, 1);
                float curr    = _grid.Get(nx, ny).height;
                float falloff = Gaussian(dist, Radius);
                _grid.Get(nx, ny).height = Mathf.Lerp(curr, avg,
                    Strength * falloff * Time.deltaTime * 5f);
            }
    }

    private float SampleAverage(int cx, int cy, int r)
    {
        float sum = 0f; int cnt = 0;
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                sum += _grid.Get(nx, ny).height; cnt++;
            }
        return cnt > 0 ? sum / cnt : 0f;
    }

    private static float Gaussian(float dist, float radius)
    {
        float s = radius * 0.5f;
        return Mathf.Exp(-(dist * dist) / (2f * s * s));
    }
}