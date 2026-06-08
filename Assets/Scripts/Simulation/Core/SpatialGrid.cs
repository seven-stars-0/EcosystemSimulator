using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Griglia spaziale per query di prossimità efficienti.
/// Rebuild ogni frame in O(n). Query in O(k) dove k = elementi nel bucket.
///
/// Usa coordinate XZ (ignora Y — gli animali si muovono sul piano XZ).
/// </summary>
public class SpatialGrid<T>
{
    private readonly float _cellSize;
    private readonly Dictionary<long, List<T>> _buckets = new();
    private readonly List<List<T>> _usedLists = new();   // per il Clear rapido

    public SpatialGrid(float cellSize) => _cellSize = cellSize;

    // ── Build ─────────────────────────────────────────────────────────────────

    public void Clear()
    {
        foreach (var list in _usedLists) list.Clear();
        _usedLists.Clear();
        // NON pulisce _buckets: i List rimangono allocati per il prossimo frame
    }

    public void Insert(Vector2 posXZ, T item)
    {
        long key = Key(posXZ);

        if (!_buckets.TryGetValue(key, out var list))
        {
            list = new List<T>();
            _buckets[key] = list;
        }

        if (list.Count == 0) _usedLists.Add(list);
        list.Add(item);
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tutti gli elementi entro <paramref name="radius"/> da <paramref name="center"/>.
    /// Riutilizza <paramref name="results"/> per evitare allocazioni.
    /// </summary>
    public void Query(Vector2 center, float radius, List<T> results)
    {
        results.Clear();
        int r = Mathf.CeilToInt(radius / _cellSize);
        int cx = Mathf.FloorToInt(center.x / _cellSize);
        int cy = Mathf.FloorToInt(center.y / _cellSize);

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                if (_buckets.TryGetValue(Key(cx + dx, cy + dy), out var list))
                    results.AddRange(list);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private long Key(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x / _cellSize);
        int y = Mathf.FloorToInt(pos.y / _cellSize);
        return Key(x, y);
    }

    private static long Key(int x, int y)
        => ((long)(x & 0x7FFFFFFF) << 32) | (uint)(y & 0x7FFFFFFF);
}