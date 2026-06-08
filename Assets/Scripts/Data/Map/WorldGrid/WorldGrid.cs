using System;
using Newtonsoft.Json;
using UnityEngine;   // solo per Mathf — accettabile in Data layer

[Serializable]
public class WorldGrid
{
    // ── Dati ─────────────────────────────────────────────────────────────────
    public int size;
    public CellData[] cells;   // flat array, index = x + y * size

    /// <summary>True when slope/gradient values match current heights (persisted on save).</summary>
    public bool slopesValid;

    // ── Costruttore ───────────────────────────────────────────────────────────
    public WorldGrid(int size)
    {
        this.size = size;
        cells = new CellData[size * size];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = new CellData();
    }

    // ── Accesso celle ────────────────────────────────────────────────────────
    public CellData Get(int x, int y) => cells[x + y * size];
    public CellData GetSafe(int x, int y)
    {
        x = Mathf.Clamp(x, 0, size - 1);
        y = Mathf.Clamp(y, 0, size - 1);
        return cells[x + y * size];
    }

    public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < size && y < size;

    // ── Conversioni coordinate ────────────────────────────────────────────────

    /// <summary>Posizione griglia → centro cella in coordinate mondo.</summary>
    public Vector3 CellToWorld(int x, int y, float cellSize, float heightScale)
        => new Vector3(x * cellSize, Get(x, y).height * heightScale, y * cellSize);

    /// <summary>Posizione mondo → cella griglia più vicina.</summary>
    public (int x, int y) WorldToCell(Vector3 worldPos, float cellSize)
        => (Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize));

    // ── Campionamento interpolato (bilinear) ──────────────────────────────────

    /// <summary>
    /// Campiona l'altezza in coordinate griglia continue (non intere).
    /// wx, wy sono in unità griglia (divide worldPos per cellSize prima di chiamare).
    /// Usato dagli agenti per stare "incollati" al terreno.
    /// </summary>
    public float SampleHeight(float wx, float wy)
    {
        int x0 = Mathf.FloorToInt(wx), y0 = Mathf.FloorToInt(wy);
        int x1 = x0 + 1, y1 = y0 + 1;
        float tx = wx - x0, ty = wy - y0;

        float h00 = GetSafe(x0, y0).height, h10 = GetSafe(x1, y0).height;
        float h01 = GetSafe(x0, y1).height, h11 = GetSafe(x1, y1).height;

        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), ty);
    }

    /// <summary>
    /// Normale interpolata in un punto continuo (per lighting e fisica agenti).
    /// </summary>
    public Vector3 SampleNormal(float wx, float wy, float cellSize, float heightScale)
    {
        float dX = (SampleHeight(wx + 1, wy) - SampleHeight(wx - 1, wy)) * 0.5f * heightScale / cellSize;
        float dZ = (SampleHeight(wx, wy + 1) - SampleHeight(wx, wy - 1)) * 0.5f * heightScale / cellSize;
        return new Vector3(-dX, 1f, -dZ).normalized;
    }

    // ── Gradiente e slope ────────────────────────────────────────────────────

    public void InvalidateSlopes() => slopesValid = false;

    /// <summary>
    /// Recomputes slopes only when heights changed since last bake (or map has no baked slopes).
    /// </summary>
    public void EnsureSlopesUpToDate()
    {
        if (slopesValid) return;
        RecalculateGradients();
    }

    /// <summary>
    /// Ricalcola gradientX, gradientY, slope per tutte le celle.
    /// Chiamato al salvataggio e quando il terreno viene modificato in editor.
    /// </summary>
    public void RecalculateGradients()
    {
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dX = (GetSafe(x + 1, y).height - GetSafe(x - 1, y).height) * 0.5f;
                float dZ = (GetSafe(x, y + 1).height - GetSafe(x, y - 1).height) * 0.5f;
                var cell = Get(x, y);
                cell.gradientX = dX;
                cell.gradientY = dZ;
                cell.slope = Mathf.Sqrt(dX * dX + dZ * dZ);
            }
        slopesValid = true;
    }

    // ── Fertilità ────────────────────────────────────────────────────────────

    /// <summary>Applica rumore Perlin alla fertilità (tool editor o procedural).</summary>
    public void ApplyFertilityNoise(float scale, float strength, float offsetX = 0f, float offsetY = 0f)
    {
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float n = Mathf.PerlinNoise(x * scale + offsetX, y * scale + offsetY);
                var cell = Get(x, y);
                cell.fertility = Mathf.Clamp01(cell.fertility + (n - 0.5f) * strength);
            }
    }

    /// <summary>Imposta fertilità uniforme su tutta la griglia.</summary>
    public void SetFertilityUniform(float value)
    {
        foreach (var c in cells) c.fertility = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Applica un pattern circolare: alta fertilità al centro, bassa ai bordi
    /// (o viceversa con strength negativo).
    /// </summary>
    public void ApplyFertilityRadial(float cx, float cy, float radius, float strength)
    {
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float t = Mathf.Clamp01(1f - dist / radius);
                var cell = Get(x, y);
                cell.fertility = Mathf.Clamp01(cell.fertility + t * strength);
            }
    }
}