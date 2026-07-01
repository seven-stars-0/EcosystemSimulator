using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class WorldGrid
{
    // Dati
    public int size;
    public CellData[] cells;   // flat array, index = x + y * size

    // Flag per quando viene modificata l'altezza in TerrainTool
    // In questo modo i gradienti vengono ricalolati al salvataggio solo se è avvenuta una modifica
    // Funge da dirty flag
    public bool slopesValid;

    public WorldGrid(int size)
    {
        // Per prevenire valori minori di 1
        this.size = Mathf.Max(1, size);

        cells = new CellData[this.size * this.size];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = new CellData();
    }

    // Accesso celle
    public CellData Get(int x, int y) => cells[x + y * size];
    public CellData GetSafe(int x, int y)
    {
        x = Mathf.Clamp(x, 0, size - 1);
        y = Mathf.Clamp(y, 0, size - 1);
        return cells[x + y * size];
    }

    public bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < size && y < size;


    // Campiona l'altezza in coordinate griglia continue
    // wx, wy sono in unità griglia (divide worldPos per cellSize prima di chiamare).
    // Usato dagli agenti per stare "incollati" al terreno.
    public float SampleHeight(float wx, float wy)
    {
        // Indici delle adiacenti
        int x0 = Mathf.FloorToInt(wx), y0 = Mathf.FloorToInt(wy);
        int x1 = x0 + 1, y1 = y0 + 1;

        // Distanza dal "basso a sinistra"
        float tx = wx - x0, ty = wy - y0;

        // Altezza delle celle adiacenti
        float h00 = GetSafe(x0, y0).height, h10 = GetSafe(x1, y0).height;
        float h01 = GetSafe(x0, y1).height, h11 = GetSafe(x1, y1).height;

        // Interpolazione bilineare
        return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), ty);
    }


    public void InvalidateSlopes() => slopesValid = false;

    // Ricalcoliamo la slope solo se è avvenuta una modifica
    public void EnsureSlopesUpToDate()
    {
        if (slopesValid) return;
        RecalculateGradients();
    }

    // Ricalcola gradientX, gradientY, slope per tutte le celle.
    // Chiamato al salvataggio e quando il terreno viene modificato in editor.
    public void RecalculateGradients()
    {
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // Usiamo il metodo delle differenze finite centrali
                float dX = (GetSafe(x + 1, y).height - GetSafe(x - 1, y).height) * 0.5f;
                float dZ = (GetSafe(x, y + 1).height - GetSafe(x, y - 1).height) * 0.5f;

                var cell = Get(x, y);
                cell.gradientX = dX;
                cell.gradientY = dZ;
                cell.slope = Mathf.Sqrt(dX * dX + dZ * dZ); // Norma euclidea
            }
        slopesValid = true;
    }


    // Applica rumore Perlin alla fertilità
    // Usato da FertilityPanel
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

    // Imposta fertilità uniforme su tutta la griglia
    public void SetFertilityUniform(float value)
    {
        foreach (var c in cells) c.fertility = Mathf.Clamp01(value);
    }

    // Applica un pattern circolare: alta fertilità al centro, bassa ai bordi (o viceversa con strength negativo).
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