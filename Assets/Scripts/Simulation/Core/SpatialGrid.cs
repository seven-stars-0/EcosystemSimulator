using System.Collections.Generic;
using UnityEngine;

// Griglia spaziale per query di prossimità efficienti.
// Rebuild ogni frame in O(n). Query in O(k) dove k = elementi nel bucket.
//
// Usa coordinate XZ (ignora Y perché gli animali si muovono sul piano XZ).
public class SpatialGrid<T>
{
    private readonly float _cellSize;
    private readonly Dictionary<long, List<T>> _buckets = new(); // Usiamo liste perché più animali possono trovarsi nella stessa cella contemporaneamente
    private readonly List<List<T>> _usedLists = new();   // per il Clear() rapido senza passare per _buckets

    public SpatialGrid(float cellSize) => _cellSize = Mathf.Max(0.01f, cellSize);  // guard: no div/0

    // Chiamato una volta per frame da SimulationRunner
    public void Clear()
    {
        foreach (var list in _usedLists) list.Clear();
        _usedLists.Clear();
        // NON pulisce _buckets: i List rimangono allocati per il prossimo frame
    }

    // Chiamato da SimulationRunner per inserire/aggiornare la posizione degli animali
    public void Insert(Vector2 posXZ, T item)
    {
        long key = Key(posXZ);

        // Se la cella XZ non ha mai contenuto un animale, viene creata
        if (!_buckets.TryGetValue(key, out var list))
        {
            list = new List<T>();
            _buckets[key] = list;
        }

        if (list.Count == 0) _usedLists.Add(list);
        list.Add(item);
    }

    // Mette in results tutti gli elementi entro radius da center
    // EcologySystem e PerceptionSystem riusano results per evitare allocazioni continue onerose
    public void Query(Vector2 center, float radius, List<T> results)
    {
        results.Clear();
        int r = Mathf.CeilToInt(radius / _cellSize); // Approssimiamo per eccesso, è più giusto così
        int cx = Mathf.FloorToInt(center.x / _cellSize);
        int cy = Mathf.FloorToInt(center.y / _cellSize);

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                // Scorriamo tutto il raggio attorno al center, e restituiamo gli animali attorno al radius
                if (_buckets.TryGetValue(Key(cx + dx, cy + dy), out var list))
                    results.AddRange(list);
    }

    private long Key(Vector2 pos)
    {
        int x = Mathf.FloorToInt(pos.x / _cellSize);
        int y = Mathf.FloorToInt(pos.y / _cellSize);
        return Key(x, y);
    }

    private static long Key(int x, int y)
        => ((long)(x & 0x7FFFFFFF) << 32) | (uint)(y & 0x7FFFFFFF);
}