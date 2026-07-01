using UnityEngine;

/// <summary>
/// Holds the active map for the current editor or simulation session.
/// UI and tools read the map here instead of passing MapData through every call chain.
/// </summary>
public class MapSession : MonoBehaviour
{
    public static MapSession Instance { get; private set; }

    public MapData CurrentMap { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetMap(MapData map) => CurrentMap = map;

    public void Clear() => CurrentMap = null;

    public void MarkDirty()
    {
        if (CurrentMap?.metadata != null)
            CurrentMap.metadata.isDirty = true;
    }
}
