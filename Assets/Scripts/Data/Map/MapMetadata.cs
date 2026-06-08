// Assets/Scripts/Data/MapMetadata.cs
using System;
using Newtonsoft.Json;

[Serializable]
public class MapMetadata
{
    public string mapName = "Nuova mappa";
    public int gridSize = 32;
    public string savedAt;

    // ── Campi runtime (non serializzati) ─────────────────────────────────────
    [JsonIgnore] public string filePath;
    [JsonIgnore] public bool isDirty;
}