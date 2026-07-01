using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel per le impostazioni di WorldCamera.
/// Ogni modifica viene applicata live alla camera (se attiva) e salvata in
/// AppSettings.camera. Il pulsante Reset ripristina i valori di default.
/// </summary>
public class CameraPanel : MonoBehaviour
{
    [Header("Reset")]
    [SerializeField] private Button resetButton;

    [Header("Orbit")]
    [SerializeField] private SliderParam orbitSpeedXSlider;
    [SerializeField] private SliderParam orbitSpeedYSlider;
    [SerializeField] private SliderParam pitchMinSlider;
    [SerializeField] private SliderParam pitchMaxSlider;

    [Header("Zoom")]
    [SerializeField] private SliderParam zoomSpeedSlider;

    [Header("Pan")]
    [SerializeField] private SliderParam panSpeedSlider;
    [SerializeField] private SliderParam panDampingSlider;
    [SerializeField] private SliderParam arrowSpeedSlider;

    [Header("POV")]
    [SerializeField] private SliderParam povEyeHeightSlider;
    [SerializeField] private SliderParam povSensitivitySlider;

    private AppSettings _settings;

    public void Bind(AppSettings settings)
    {
        _settings = settings;
        var c = settings.camera;
        var cam = WorldSession.Instance?.Camera;

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetToDefaults);
        }

        // ── Orbit ──────────────────────────────────────────────────────────────
        orbitSpeedXSlider.Setup("Orbit speed H", c.orbitSpeedX, 0.05f, 2f,
            v => { c.orbitSpeedX = v; cam?.ApplySettings(c); });

        orbitSpeedYSlider.Setup("Orbit speed V", c.orbitSpeedY, 0.05f, 2f,
            v => { c.orbitSpeedY = v; cam?.ApplySettings(c); });

        pitchMinSlider.Setup("Pitch min (°)", c.pitchMin, 0f, 44f,
            v => { c.pitchMin = v; cam?.ApplySettings(c); });

        pitchMaxSlider.Setup("Pitch max (°)", c.pitchMax, 45f, 89f,
            v => { c.pitchMax = v; cam?.ApplySettings(c); });

        // ── Zoom ───────────────────────────────────────────────────────────────
        zoomSpeedSlider.Setup("Zoom speed", c.zoomSpeed, 0.5f, 15f,
            v => { c.zoomSpeed = v; cam?.ApplySettings(c); });

        // ── Pan ────────────────────────────────────────────────────────────────
        panSpeedSlider.Setup("Pan speed", c.panSpeed, 0.005f, 0.3f,
            v => { c.panSpeed = v; cam?.ApplySettings(c); });

        panDampingSlider.Setup("Pan damping", c.panDamping, 1f, 25f,
            v => { c.panDamping = v; cam?.ApplySettings(c); });

        arrowSpeedSlider.Setup("Arrow speed", c.arrowSpeed, 2f, 80f,
            v => { c.arrowSpeed = v; cam?.ApplySettings(c); });

        // ── POV ────────────────────────────────────────────────────────────────
        povEyeHeightSlider.Setup("Eye height", c.povEyeHeight, 0.1f, 4f,
            v => { c.povEyeHeight = v; cam?.ApplySettings(c); });

        povSensitivitySlider.Setup("POV sensitivity", c.povSensitivity, 0.02f, 1.5f,
            v => { c.povSensitivity = v; cam?.ApplySettings(c); });
    }

    public void ResetToDefaults()
    {
        if (_settings == null) return;
        UIManager.Instance.ShowConfirm(
            "Reset camera settings to their default values?",
            onConfirm: () =>
            {
                _settings.camera = new CameraSettings();                       // default
                WorldSession.Instance?.Camera?.ApplySettings(_settings.camera); // applica live
                AppSettingsManager.Instance?.Save();                            // persisti
                Bind(_settings);                                                // ridisegna gli slider
            });
    }
}
