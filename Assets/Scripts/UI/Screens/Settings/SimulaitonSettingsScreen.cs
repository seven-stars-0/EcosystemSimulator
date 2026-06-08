using UnityEngine;
using UnityEngine.UI;

public class SimulationSettingsScreen : UIScreen
{
    [Header("Navigation")]
    [SerializeField] private Button backBtn;

    [Header("Tab system")]
    [SerializeField] private TabPanel[] tabPanels;   // 0=Parametri, 1=Fertilità

    [Header("Pannelli")]
    [SerializeField] private ParameterPanel parametersPanel;
    [SerializeField] private FertilityPanel fertilityPanel;

    private MapData _currentMap;

    protected override void Awake()
    {
        base.Awake();

        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());

        for (int i = 0; i < tabPanels.Length; i++)
        {
            int captured = i;
            tabPanels[i].tab.onClick.AddListener(() => ShowTab(captured));
        }
    }

    public void PrepareForMap(MapData data) => _currentMap = data;

    protected override void OnShow()
    {
        // Blocca la camera mentre si è nelle impostazioni
        InputGuard.CameraInputBlocked = true;

        if (_currentMap?.simulationSettings == null) return;

        var s = _currentMap.simulationSettings;
        parametersPanel.Bind(s);
        fertilityPanel.Bind(_currentMap.grid);

        ShowTab(0);
    }

    protected override void OnHide()
    {
        // Sblocca la camera quando si esce
        InputGuard.CameraInputBlocked = false;

        // Qualsiasi modifica nelle settings segna la mappa come dirty
        if (_currentMap != null)
            _currentMap.metadata.isDirty = true;
    }

    private void ShowTab(int index)
    {
        for (int j = 0; j < tabPanels.Length; j++)
            tabPanels[j].Hide();
        tabPanels[index].Show();
    }
}