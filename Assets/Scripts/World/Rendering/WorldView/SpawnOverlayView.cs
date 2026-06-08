using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpawnOverlayView : MonoBehaviour, IWorldView
{
    [Header("Colori — richiede materiale Transparent con vertex color")]
    public Color validColor = new Color(0.15f, 0.90f, 0.15f, 0.40f);
    public Color invalidColor = new Color(0.90f, 0.15f, 0.15f, 0.40f);

    [Tooltip("Offset Y per evitare z-fighting col terreno")]
    public float surfaceOffset = 0.06f;

    private MeshFilter _mf;
    private Mesh _mesh;
    private WorldGrid _grid;
    private RenderConfig _cfg;

    private void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mesh = new Mesh { name = "SpawnOverlayMesh" };
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }

    // ── IWorldView ────────────────────────────────────────────────────────────

    public void Build(WorldGrid grid, RenderConfig cfg)
    {
        _grid = grid;
        _cfg = cfg;
        Rebuild();
    }

    public void Refresh(WorldGrid grid, RenderConfig cfg)
    {
        _grid = grid;
        _cfg = cfg;
        Rebuild();
    }

    public void SetVisible(bool v) => gameObject.SetActive(v);

    // ── Costruzione mesh ──────────────────────────────────────────────────────

    private void Rebuild()
    {
        if (_grid == null) return;

        int n = _grid.size;
        int cells = (n - 1) * (n - 1);

        // 4 vertici NON condivisi per quad — ogni cella ha il suo colore indipendente
        var verts = new Vector3[cells * 4];
        var cols = new Color[cells * 4];
        var tris = new int[cells * 6];

        int vi = 0, ti = 0;

        for (int x = 0; x < n - 1; x++)
        {
            for (int y = 0; y < n - 1; y++)
            {
                // La cella è rappresentata dal vertice bottom-left (x,y)
                // → stessa convenzione del raycast in WorldEditor (RoundToInt)
                Color color = SpawnTool.IsCellValidForSpawn(_grid.Get(x, y))
                    ? validColor
                    : invalidColor;

                float x0 = x * _cfg.cellSize, x1 = (x + 1) * _cfg.cellSize;
                float z0 = y * _cfg.cellSize, z1 = (y + 1) * _cfg.cellSize;

                float y00 = _grid.Get(x, y).height * _cfg.heightScale + surfaceOffset;
                float y10 = _grid.Get(x + 1, y).height * _cfg.heightScale + surfaceOffset;
                float y01 = _grid.Get(x, y + 1).height * _cfg.heightScale + surfaceOffset;
                float y11 = _grid.Get(x + 1, y + 1).height * _cfg.heightScale + surfaceOffset;

                // vi   = (x,   y  ) BL  — vi+1 = (x+1, y  ) BR
                // vi+2 = (x,   y+1) TL  — vi+3 = (x+1, y+1) TR
                verts[vi] = new Vector3(x0, y00, z0);
                verts[vi + 1] = new Vector3(x1, y10, z0);
                verts[vi + 2] = new Vector3(x0, y01, z1);
                verts[vi + 3] = new Vector3(x1, y11, z1);

                cols[vi] = cols[vi + 1] = cols[vi + 2] = cols[vi + 3] = color;

                // Winding identico a TerrainView (normale verso +Y)
                tris[ti++] = vi; tris[ti++] = vi + 2; tris[ti++] = vi + 1;
                tris[ti++] = vi + 1; tris[ti++] = vi + 2; tris[ti++] = vi + 3;

                vi += 4;
            }
        }

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.colors = cols;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _mf.mesh = _mesh;
    }
}