using UnityEngine;
using UnityEngine.UI;

public class TerrainToolBar : MonoBehaviour
{
    [Header("Pennello")]
    [SerializeField] private SliderParam strengthSlider;
    [SerializeField] private SliderParam radiusSlider;
    [SerializeField] private Toggle raiseToggle;
    [SerializeField] private Toggle smoothToggle;

    [Header("Generazione Perlin")]
    [SerializeField] private SliderParam scaleSlider;       // frequenza del rumore
    [SerializeField] private SliderParam amplitudeSlider;   // altezza massima (e profondità dell'acqua)
    [SerializeField] private Button      perlinButton;      // applica Perlin all'intera mappa
    [SerializeField] private Button      resetButton;       // azzera tutte le altezze

    private TerrainTool _tool;
    private const float SeaLevel = 0.30f;   // soglia acqua sul rumore [0,1]: piu' basso = meno acqua
    private const float MaxDepth = 0.25f;   // profondita' massima dell'acqua

    private float _scale = 0.05f;
    private float _amplitude = 1f;

    public void Bind(TerrainTool tool)
    {
        _tool = tool;

        // Colleghiamo i valori degli slider e dei toggle a quelli in TerrainTool
        strengthSlider.Setup("Strength", tool.Strength, 0.01f, 1f, v => _tool.Strength = v);
        radiusSlider.Setup("Radius", tool.Radius, 1f, 10f, v => _tool.Radius = Mathf.RoundToInt(v));

        raiseToggle.onValueChanged.RemoveAllListeners();
        smoothToggle.onValueChanged.RemoveAllListeners();
        raiseToggle.isOn = tool.Raise;
        smoothToggle.isOn = tool.Smooth;

        raiseToggle.onValueChanged.AddListener(v => _tool.Raise = v);
        smoothToggle.onValueChanged.AddListener(v => _tool.Smooth = v);

        // Perlin
        scaleSlider.Setup("Scale", _scale, 0.01f, 0.15f, v => _scale = v);
        amplitudeSlider.Setup("Height", _amplitude, 0.1f, 2f, v => _amplitude = v);

        perlinButton.onClick.RemoveAllListeners();
        perlinButton.onClick.AddListener(ApplyPerlin);
        
        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(ResetHeights);
    }

    private WorldGrid Grid => MapSession.Instance != null ? MapSession.Instance.CurrentMap?.grid : null;

    private void ApplyPerlin()
    {
        var grid = Grid;
        if (grid == null) return;

        // Offset casuale: ogni applicazione genera un terreno diverso con la stessa scala.
        float offX = Random.Range(0f, 1000f);
        float offY = Random.Range(0f, 1000f);

        for (int x = 0; x < grid.size; x++)
            for (int y = 0; y < grid.size; y++)
            {
                float n = Mathf.PerlinNoise(x * _scale + offX, y * _scale + offY);   // [0,1]

                // Solo il rumore sotto SeaLevel diventa acqua (acqua è cella con height < 0)
                // Questo serve perché altrimenti c'era troppa acqua e troppa poca terra
                //
                // La terra scala con _amplitude, mentre l'acqua resta sempre poco profonda (al massimo -MaxDepth).
                float t = n - SeaLevel;
                grid.Get(x, y).height = t >= 0f ? t * _amplitude : Mathf.Max(t, -MaxDepth);
            }

        SmoothHeights(grid, 2);
        RefreshTerrain(grid);
    }

    // Rende le celle tutte ad altezza 0
    private void ResetHeights()
    {
        var grid = Grid;
        if (grid == null) return;
        foreach (var c in grid.cells) c.height = 0f;

        RefreshTerrain(grid);
    }

    // Box-blur 3x3 ripetuto: media ogni cella coi vicini per ottenere un terreno piu' liscio
    // Iteriamo un certo numero di volte (noi facciamo solo 2 volte) per migliorare il risultato
    // MOLTO ONEROSO, VEDI SE PUò ESSERE MIGLIORATO (no)
    private static void SmoothHeights(WorldGrid grid, int iterations)
    {
        int n = grid.size;
        var tmp = new float[n * n];

        for (int it = 0; it < iterations; it++)
        {
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                {
                    // Calcoliamo l'altezza media nei dintorni della cella corrente
                    float sum = 0f; int cnt = 0;

                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue; // Celle fuori dai bordi
                            sum += grid.Get(nx, ny).height; cnt++;
                        }

                    tmp[x + y * n] = sum / cnt;
                }

            // Applichiamo i risultati
            for (int i = 0; i < n * n; i++) grid.cells[i].height = tmp[i];
        }
    }

    // Chiamato dopo ApplyPerlin e ResetHeights
    // Ricostruzione mesh + riallineamento entità/ostacoli + dirty
    private void RefreshTerrain(WorldGrid grid)
    {
        // grid.InvalidateSlopes();
        MapSession.Instance?.MarkDirty();

        var ws = WorldSession.Instance;
        if (ws == null) return;

        ws.Renderer.MarkDirty(DirtyFlags.Terrain | DirtyFlags.SpawnOverlay);
        ws.Builder.SyncEntitiesHeight();
    }
}
