using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopulationGraphHUD : MonoBehaviour
{
    [Header("Statistiche")]
    [SerializeField] private TMP_Text statsLabel;

    [Header("Grafico")]
    [SerializeField] private RawImage chart;
    [SerializeField] private GameObject chartRoot;   // radice da attivare/disattivare (fallback: il GO della RawImage)

    [Header("Parametri grafico")]
    [SerializeField] private float sampleInterval = 0.5f;   // secondi tra un campione e l'altro (indipendente dal log)
    [SerializeField] private int historyLength = 360;       // n. campioni tenuti = larghezza in pixel della texture
    [SerializeField] private int graphHeight = 150;         // altezza in pixel della texture
    [Range(1, 5)]
    [SerializeField] private int lineThickness = 2;

    // Serie scorrevoli dei conteggi
    private readonly List<int> _prey = new();
    private readonly List<int> _pred = new();

    private float _timer;              // accumulatore per il campionamento
    private float _lastElapsed = -1f;  // per rilevare il restart della simulazione (tempo che torna indietro)

    private Texture2D _tex;
    private Color[] _px; // buffer pixel riusato a ogni Redraw (niente alloc per frame)

    private static readonly Color BG   = new Color(0.06f, 0.06f, 0.09f, 0.85f);
    private static readonly Color GRID = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color PREY = new Color(0.45f, 0.90f, 0.50f, 1f);
    private static readonly Color PRED = new Color(0.95f, 0.42f, 0.42f, 1f);

    private void Awake()
    {
        EnsureTexture();
        if (chart != null) { chart.texture = _tex; chart.color = Color.white; }
    }

    private void Update()
    {
        var run = SimulationRunner.Instance;
        if (run == null) return;

        // Se il tempo trascorso è tornato indietro, siamo in una nuova simulazione, quindi ridisegnamo il grafico
        if (run.ElapsedTime < _lastElapsed - 0.01f)
        {
            _prey.Clear(); _pred.Clear();
            if (IsChartShown()) Redraw();
        }
        _lastElapsed = run.ElapsedTime;

        // Etichetta testuale, sempre visibile
        if (statsLabel != null)
        {
            int sec = Mathf.Max(0, Mathf.FloorToInt(run.ElapsedTime));
            statsLabel.text =
                $"<color=#73e680>Prey: {run.PreyCount}</color>\n" +
                $"<color=#f26b6b>Predator: {run.PredatorCount}</color>\n" +
                $"Plant: {run.PlantCount}\n" +
                $"Time: {sec / 60}:{sec % 60:00}";
        }

        // Eseguiamo un campionamento ogni sampleInterval secondi
        _timer += Time.unscaledDeltaTime;
        if (_timer < sampleInterval) return;
        _timer = 0f;

        // Prendiamo i dati da SimulationRunner SEMPRE
        Push(_prey, run.PreyCount);
        Push(_pred, run.PredatorCount);
        // Se il grafico è mostrato, lo ridisegnamo
        if (IsChartShown()) Redraw();
    }

    // Aggiunge un campione e mantiene la serie lunga al massimo historyLength (FIFO)
    private void Push(List<int> series, int value)
    {
        series.Add(value);
        while (series.Count > historyLength) series.RemoveAt(0);
    }

    public void ToggleChart() => SetChartVisible(!IsChartShown());

    public void SetChartVisible(bool show)
    {
        var go = ChartObject();
        if (go == null) return;
        go.SetActive(show);
        if (show) Redraw(); // Ridisegna all'apertura
    }

    private GameObject ChartObject()
        => chartRoot != null ? chartRoot : (chart != null ? chart.gameObject : null);

    private bool IsChartShown()
    {
        var go = ChartObject();
        return go != null && go.activeInHierarchy;
    }

    // Crea/ricrea la texture solo se la dimensione richiesta e' cambiata.
    private void EnsureTexture()
    {
        int w = Mathf.Max(8, historyLength);
        int h = Mathf.Max(8, graphHeight);
        if (_tex != null && _tex.width == w && _tex.height == h) return;
        _tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        _px = new Color[w * h];
        if (chart != null) chart.texture = _tex;
    }

    private void Redraw()
    {
        EnsureTexture();
        int w = _tex.width, h = _tex.height;

        // Sfondo + 3 linee di griglia orizzontali (a 1/4, 2/4, 3/4 dell'altezza).
        for (int i = 0; i < _px.Length; i++) _px[i] = BG;
        for (int gln = 1; gln < 4; gln++)
        {
            int baseI = (h * gln / 4) * w;
            for (int x = 0; x < w; x++) _px[baseI + x] = GRID;
        }

        // Scala verticale automatica basata sul picco delle due serie + 15% di margine per estetica
        float max = 1f;
        for (int i = 0; i < _prey.Count; i++) if (_prey[i] > max) max = _prey[i];
        for (int i = 0; i < _pred.Count; i++) if (_pred[i] > max) max = _pred[i];
        max *= 1.15f;

        PlotI(_px, w, h, _prey, max, PREY, lineThickness); // Prede
        PlotI(_px, w, h, _pred, max, PRED, lineThickness); // Predatori

        _tex.SetPixels(_px);
        _tex.Apply(false);
    }

    // Disegna una serie come spezzata, mappando l'indice sull'asse X (a piena
    // larghezza historyLength) e il valore sull'asse Y (scala 'max').
    private void PlotI(Color[] px, int w, int h, List<int> data, float max, Color col, int thick)
    {
        int n = data.Count;
        if (n < 2) return;
        int denom = (historyLength > 1)? historyLength - 1 : 1;
        int prevX = 0, prevY = ValueToY(data[0], max, h);
        for (int i = 1; i < n; i++)
        {
            int x = (w - 1) * i / denom;
            int y = ValueToY(data[i], max, h);
            DrawLine(px, w, h, prevX, prevY, x, y, col, thick);
            prevX = x; prevY = y;
        }
    }

    private static int ValueToY(float value, float max, int h)
    {
        float t = Mathf.Clamp01(value / Mathf.Max(1f, max));
        return Mathf.Clamp(Mathf.RoundToInt(t * (h - 1)), 0, h - 1);
    }

    // Punto "spesso": quadrato thick x thick centrato su (cx,cy), con bounds check.
    private static void Plot(Color[] px, int w, int h, int cx, int cy, Color col, int thick)
    {
        int r = thick / 2;
        for (int oy = -r; oy <= r; oy++)
            for (int ox = -r; ox <= r; ox++)
            {
                int x = cx + ox, y = cy + oy;
                if ((uint)x < (uint)w && (uint)y < (uint)h) px[y * w + x] = col;
            }
    }

    // Segmento tra due punti con l'algoritmo di Bresenham (solo interi).
    private static void DrawLine(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color col, int thick)
    {
        int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Plot(px, w, h, x0, y0, col, thick);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}
