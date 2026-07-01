using System;
using Newtonsoft.Json;

[Serializable]
public class MapMetadata
{
    public string mapName = "New map";
    public int gridSize = 32;
    public string savedAt;
    public int schemaVersion = 0;   // schema dei parametri; 0 = mappa pre-versioning

    // Campi runtime
    [JsonIgnore] public string filePath;
    [JsonIgnore] public bool isDirty;
}
