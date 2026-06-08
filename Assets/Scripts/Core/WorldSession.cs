using UnityEngine;

/// <summary>
/// Single entry point for building/tearing the 3D world.
/// UI and tools depend only on this facade.
/// </summary>
public class WorldSession : MonoBehaviour
{
    public static WorldSession Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] private WorldBuilder  worldBuilder;
    [SerializeField] private WorldEditor   worldEditor;
    [SerializeField] private WorldRenderer worldRenderer;
    [SerializeField] private WorldCamera   worldCamera;

    public WorldBuilder  Builder  => worldBuilder;
    public WorldEditor   Editor   => worldEditor;
    public WorldRenderer Renderer => worldRenderer;
    public WorldCamera   Camera   => worldCamera;
    public bool          IsActive { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void EnterEditor(MapData map)
    {
        MapSession.Instance.SetMap(map);
        worldBuilder.BuildForEditor(map);
        IsActive = true;
    }

    public void EnterSimulation(MapData map)
    {
        MapSession.Instance.SetMap(map);
        worldBuilder.BuildForSimulation(map);
        IsActive = true;
    }

    public void Exit()
    {
        worldEditor.SetTool(null);
        worldEditor.SetEnabled(false);
        worldBuilder.TearDown();
        worldCamera.SetFollowTarget(null);
        MapSession.Instance.Clear();
        IsActive = false;
    }
}