using UnityEngine;

public class TerrainTool : IEditorTool
{
    public float Strength = 0.2f;
    public int   Radius   = 3;
    public bool  Raise    = true;
    public bool  Smooth   = false;
    private WorldGrid Grid => MapSession.Instance != null ? MapSession.Instance.CurrentMap?.grid : null;

    public void OnActivate()   { }
    public void OnDeactivate() { }

    public void OnClick(CellHit hit)     => Apply(hit.x, hit.y);
    public void OnDragStart(CellHit hit) => Apply(hit.x, hit.y);
    public void OnDrag(CellHit hit)      => Apply(hit.x, hit.y);

    public void OnDragEnd(CellHit hit)
    {
        // Grid.RecalculateGradients();
        WorldSession.Instance.Renderer.MarkDirty(DirtyFlags.SpawnOverlay);
    }

    private void Apply(int cx, int cy)
    {
        if (Smooth) ApplySmooth(cx, cy);
        else        ApplyRaiseLower(cx, cy);

        // Grid.InvalidateSlopes();
        MapSession.Instance?.MarkDirty();

        WorldSession.Instance.Renderer.MarkDirty(DirtyFlags.Terrain);
        WorldSession.Instance.Builder.SyncEntitiesHeight();
    }

    // Usando cx e cy come cella centrale (risultato del raycast in WorldEditor),
    // modifichiamo l'altezza delle celle adiacenti contenute nel radius selezionato dall'utente con 
    private void ApplyRaiseLower(int cx, int cy)
    {
        // Decidiamio se alzare o abbassare il terreno
        float delta = Strength * (Raise ? 1f : -1f) * Time.deltaTime; // Usiamo dt per alzare il terreno gradualmente

        for (int dx = -Radius; dx <= Radius; dx++)
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!Grid.IsInside(nx, ny)) continue; // Per le zone vicino ai bordi

                float dist    = Mathf.Sqrt(dx * dx + dy * dy); // Distanza dal centro del cerchio
                float falloff = Gaussian(dist, Radius);

                // Modifichiamo l'altezza della cella in analisi con il falloff Gaussiano
                Grid.Get(nx, ny).height += delta * falloff;
            }
    }

    // Calcoliamo l'altezza media delle celle comprese nel raggio e modifichiamo l'altezza
    // per conformarla al valore ottenuto
    private void ApplySmooth(int cx, int cy)
    {
        for (int dx = -Radius; dx <= Radius; dx++)
            for (int dy = -Radius; dy <= Radius; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!Grid.IsInside(nx, ny)) continue;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > Radius) continue; // Non consideriamo le celle al di fuori del raggio

                float avg     = SampleAverage(nx, ny, 1);
                float curr    = Grid.Get(nx, ny).height;
                float falloff = Gaussian(dist, Radius);

                // Aumentiamo il valore gradualmente (con dt) grazie  all'interpolazione lineare
                Grid.Get(nx, ny).height = Mathf.Lerp(curr, avg,
                    Strength * falloff * Time.deltaTime * 5f); // 5f serve per rendere il processo più veloce
            }
    }

    // Iteriamo tutte le celle entro il raggio, calcolando l'altezza media
    // Usiamo solo r = 1
    private float SampleAverage(int cx, int cy, int r)
    {
        float sum = 0f; int cnt = 0;
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!Grid.IsInside(nx, ny)) continue;

                sum += Grid.Get(nx, ny).height; cnt++;
            }
        return (cnt > 0) ? sum / cnt : 0f;
    }

    private static float Gaussian(float dist, float radius)
    {
        float s = radius * 0.5f; // Deviazione standard (σ)
        return Mathf.Exp(-(dist * dist) / (2f * s * s));
    }
}
