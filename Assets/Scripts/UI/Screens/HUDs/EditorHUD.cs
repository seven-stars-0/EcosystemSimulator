using UnityEngine;
using UnityEngine.UI;

public class EditorHUD : UIScreen
{
    [Header("Top Bar")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button saveButton;

    [Header("Tool Selection (ToggleGroup)")]
    [SerializeField] private ToggleGroup toolToggleGroup;
    [SerializeField] private Toggle      terrainToggle;
    [SerializeField] private Toggle      spawnToggle;
    [SerializeField] private Button      settingsButton;

    [Header("Toolbars")]
    [SerializeField] private TerrainToolBar terrainToolBar;
    [SerializeField] private SpawnToolBar   spawnToolBar;

    private MapData     _currentMap;
    private TerrainTool _terrainTool;
    private SpawnTool   _spawnTool;

    // Binding dei bottoni
    protected override void Awake()
    {
        base.Awake();
        backButton    .onClick.AddListener(OnBack);
        saveButton    .onClick.AddListener(OnSave);
        settingsButton.onClick.AddListener(OnSettings);
        terrainToggle .onValueChanged.AddListener(on => { if (on) ActivateTool(EditorToolType.Terrain); });
        spawnToggle   .onValueChanged.AddListener(on => { if (on) ActivateTool(EditorToolType.Spawn);   });
    }

    public void PrepareForMap(MapData data) => _currentMap = data;

    protected override void OnShow()
    {
        if (_currentMap == null) return;

        WorldSession.Instance.EnterEditor(_currentMap);

        _terrainTool = new TerrainTool();
        _spawnTool   = new SpawnTool();

        terrainToggle.SetIsOnWithoutNotify(true);
        spawnToggle.SetIsOnWithoutNotify(false);
        ActivateTool(EditorToolType.Terrain); // Di default si apre con TerrainTool
    }

    protected override void OnHide()
    {
        WorldSession.Instance?.Editor.SetEnabled(false);
    }

    // Attiva il tool selezionato, disattivando l'altro
    private void ActivateTool(EditorToolType type)
    {
        if (_terrainTool == null) return;

        terrainToolBar.gameObject.SetActive(type == EditorToolType.Terrain);
        spawnToolBar  .gameObject.SetActive(type == EditorToolType.Spawn);

        var editor = WorldSession.Instance.Editor;

        switch (type)
        {
            case EditorToolType.Terrain:
                terrainToolBar.Bind(_terrainTool);
                editor.SetTool(_terrainTool);
                break;
            case EditorToolType.Spawn:
                spawnToolBar.Bind(_spawnTool);
                editor.SetTool(_spawnTool);
                break;
        }
    }

    // Configura SimulationSettingsScreen con i dati in MapData
    private void OnSettings()
    {
        var s = UIManager.Instance.GetScreen<SimulationSettingsScreen>();
        s.PrepareForMap(_currentMap);
        UIManager.Instance.Show(s);
    }
    
    // Chiede conferma di uscire se sono state apportate modifiche non salvate, altrimenti esce direttamente
    private void OnBack()
    {
        if (_currentMap?.metadata.isDirty == true)
            UIManager.Instance.ShowConfirm(
                "You have unsaved changes.\nLeave without saving?",
                onConfirm: ExitEditor);
        else
            ExitEditor();
    }

    // Apre EnterValuePopup per inserire il nome della mappa
    private void OnSave()
    {
        if (_currentMap == null) return;
        UIManager.Instance.RequestString(
            "Map name",
            _currentMap.metadata.mapName,
            name =>
            {
                _currentMap.metadata.mapName = name;
                MapSaveManager.Instance.Save(_currentMap);
                ExitEditor();
            }
        );
    }

    private void ExitEditor()
    {
        _terrainTool = null;
        _spawnTool   = null;
        _currentMap  = null;
        WorldSession.Instance.Exit();
        UIManager.Instance.GoBack();
    }
}

public enum EditorToolType { Terrain, Spawn }