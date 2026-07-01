using UnityEngine;

public class SimulationSettingsScreen : ISettingsScreen
{
    [Header("Pannelli")]
    [SerializeField] private ParameterPanel parametersPanel;
    [SerializeField] private FertilityPanel fertilityPanel;

    private MapData _currentMap;

    public void PrepareForMap(MapData data) => _currentMap = data;

    protected override void OnBind()
    {
        if (_currentMap?.simulationSettings == null) return;
        parametersPanel.Bind(_currentMap.simulationSettings);
        fertilityPanel.Bind(_currentMap.grid);
    }

    protected override void OnUnbind()
    {
        // Qualsiasi modifica segna la mappa come dirty
        if (_currentMap != null)
            _currentMap.metadata.isDirty = true;
    }
}