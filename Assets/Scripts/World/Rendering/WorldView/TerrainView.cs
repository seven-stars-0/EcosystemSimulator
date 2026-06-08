using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainView : MonoBehaviour, IWorldView
{
    private MeshFilter _mf;
    private MeshCollider _mc;
    private Mesh _mesh;

    // ── Palette colori ────────────────────────────────────────────────────────
    // Modificabile dall'Inspector per tweaking rapido
    [Header("Color Palette")]
    public Color deepWater = new Color(0.10f, 0.25f, 0.55f);
    public Color shallowWater = new Color(0.20f, 0.45f, 0.70f);
    public Color sand = new Color(0.76f, 0.70f, 0.50f);
    public Color grass = new Color(0.40f, 0.60f, 0.30f);
    public Color dryGrass = new Color(0.55f, 0.58f, 0.32f);
    public Color rock = new Color(0.45f, 0.40f, 0.35f);
    public Color highRock = new Color(0.55f, 0.52f, 0.48f);
    public Color snow = new Color(0.92f, 0.93f, 0.95f);

    private void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mc = GetComponent<MeshCollider>();
        _mesh = new Mesh { name = "TerrainMesh" };
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    public void Build(WorldGrid grid, RenderConfig cfg)
    {
        Rebuild(grid, cfg);
    }

    // Refresh = rebuild completo per il terreno (ogni modifica cambia geometria)
    public void Refresh(WorldGrid grid, RenderConfig cfg)
    {
        Rebuild(grid, cfg);
    }

    public void SetVisible(bool v) => gameObject.SetActive(v);

    private void Rebuild(WorldGrid grid, RenderConfig cfg)
    {
        int n = grid.size;

        var vertices = new Vector3[n * n];
        var colors = new Color[n * n];
        var triangles = new int[(n - 1) * (n - 1) * 6];
        var uvs = new Vector2[n * n];

        int ti = 0;

        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
            {
                int i = x + y * n;
                var cell = grid.Get(x, y);
                float worldY = cell.height * cfg.heightScale;

                vertices[i] = new Vector3(x * cfg.cellSize, worldY, y * cfg.cellSize);
                colors[i] = HeightToColor(cell.height, cfg.heightScale);
                uvs[i] = new Vector2((float)x / (n - 1), (float)y / (n - 1));
            }

        for (int x = 0; x < n - 1; x++)
            for (int y = 0; y < n - 1; y++)
            {
                int i = x + y * n;
                triangles[ti++] = i;
                triangles[ti++] = i + n;
                triangles[ti++] = i + 1;
                triangles[ti++] = i + 1;
                triangles[ti++] = i + n;
                triangles[ti++] = i + n + 1;
            }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.colors = colors;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _mf.mesh = _mesh;
        _mc.sharedMesh = _mesh;   // aggiorna collider per il raycast
    }

    // ── Color gradient ────────────────────────────────────────────────────────
    // Il gradient usa heightScale per ricavare altezze mondo significative.
    // Le soglie sono in unità logiche (come height in CellData).
    private Color HeightToColor(float h, float hs)
    {
        // Acqua
        if (h < -0.5f) return deepWater;
        if (h < 0f) return Color.Lerp(deepWater, shallowWater, (h + 0.5f) / 0.5f);

        // Spiaggia / transizione acqua-terra
        if (h < 0.15f) return Color.Lerp(shallowWater, sand, h / 0.15f);
        if (h < 0.25f) return Color.Lerp(sand, grass, (h - 0.15f) / 0.1f);

        // Verde
        if (h < 0.55f) return grass;
        if (h < 0.65f) return Color.Lerp(grass, dryGrass, (h - 0.55f) / 0.1f);

        // Roccia
        if (h < 0.80f) return Color.Lerp(dryGrass, rock, (h - 0.65f) / 0.15f);
        if (h < 0.90f) return rock;
        if (h < 0.95f) return Color.Lerp(rock, highRock, (h - 0.90f) / 0.05f);

        // Neve
        return Color.Lerp(highRock, snow, (h - 0.95f) / 0.05f);
    }
}