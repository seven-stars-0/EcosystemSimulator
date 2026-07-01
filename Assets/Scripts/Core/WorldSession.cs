using UnityEngine;

public class WorldSession : MonoBehaviour
{
    public static WorldSession Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] private WorldBuilder worldBuilder;
    [SerializeField] private WorldEditor worldEditor;
    [SerializeField] private WorldRenderer worldRenderer;
    [SerializeField] private WorldCamera worldCamera;

    public WorldBuilder Builder => worldBuilder;
    public WorldEditor Editor => worldEditor;
    public WorldRenderer Renderer => worldRenderer;
    public WorldCamera Camera => worldCamera;
    public bool IsActive { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void EnterEditor(MapData map)
    {
        MapSession.Instance.SetMap(map);
        worldBuilder.BuildForEditor(map);
        FinishEnter();
    }

    public void EnterSimulation(MapData map)
    {
        MapSession.Instance.SetMap(map);
        worldBuilder.BuildForSimulation(map);
        FinishEnter();
    }

    // Passi comuni a editor e simulazione: applica skybox e camera dalle AppSettings.
    private void FinishEnter()
    {
        IsActive = true;
        AppSettingsManager.Instance?.ApplySkybox();
        AppSettingsManager.Instance?.ApplyCameraSettings();
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
