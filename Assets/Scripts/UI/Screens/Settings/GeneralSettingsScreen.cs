using UnityEngine;

/// <summary>
/// Schermata impostazioni globali (camera, rendering, estetica).
/// Non è legata a una mappa specifica: legge/scrive AppSettings.
/// </summary>
public class GeneralSettingsScreen : ISettingsScreen
{
    [Header("Pannelli")]
    [SerializeField] private CameraPanel cameraPanel;
    [SerializeField] private AestheticsPanel aestheticsPanel;

    protected override void OnBind()
    {
        var settings = AppSettingsManager.Instance.Settings;
        cameraPanel.Bind(settings);
        aestheticsPanel.Bind(settings);
    }

    protected override void OnUnbind()
    {
        // Salva immediatamente: le impostazioni globali persistono tra sessioni
        AppSettingsManager.Instance.Save();
    }
}