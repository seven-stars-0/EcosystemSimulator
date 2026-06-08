using UnityEngine;
using UnityEngine.UI;

public class MapSelectionScreen : UIScreen
{
    [SerializeField] private Button    backButton;
    [SerializeField] private Button    newMapButton;
    [SerializeField] private Transform listContent;
    [SerializeField] private MapRow    mapRowPrefab;

    protected override void Awake()
    {
        base.Awake();
        backButton  .onClick.AddListener(() => UIManager.Instance.GoBack());
        newMapButton.onClick.AddListener(OnNewMap);
    }

    protected override void OnShow() => RefreshList();

    private void RefreshList()
    {
        foreach (Transform child in listContent) Destroy(child.gameObject);

        foreach (var meta in MapSaveManager.Instance.LoadAllMetadata())
        {
            var row      = Instantiate(mapRowPrefab, listContent);
            var captured = meta;
            row.Initialize(captured,
                onPlay:   () => OnPlay(captured),
                onEdit:   () => OnEdit(captured),
                onDelete: () => OnDelete(captured));
        }
    }

    // ── Azioni ────────────────────────────────────────────────────────────────

    private void OnNewMap()
    {
        UIManager.Instance.RequestInt(
            "World size",
            initial: 64, min: 16, max: 128,
            onConfirm: gridSize =>
            {
                // Nome temporaneo — cambiato al primo Save
                var data = MapData.CreateEmpty("New map", gridSize);
                OpenEditor(data);
            }
        );
    }

    private void OnPlay(MapMetadata meta)
    {
        var data = MapSaveManager.Instance.Load(meta.filePath);

        WorldSession.Instance.EnterSimulation(data);
        SimulationSession.Instance.Begin(data);

        UIManager.Instance.Show<SimulationHUD>();
    }

    private void OnEdit(MapMetadata meta)
        => OpenEditor(MapSaveManager.Instance.Load(meta.filePath));

    private void OnDelete(MapMetadata meta)
    {
        UIManager.Instance.ShowConfirm(
            $"Delete \"{meta.mapName}\"?\nThis cannot be undone.",
            onConfirm: () => { MapSaveManager.Instance.Delete(meta); RefreshList(); }
        );
    }

    private void OpenEditor(MapData data)
    {
        var hud = UIManager.Instance.GetScreen<EditorHUD>();
        hud.PrepareForMap(data);
        UIManager.Instance.Show(hud);
    }
}