using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExtinctionPopup : Popup
{
    [Header("Stats")]
    [SerializeField] private TMP_Text elapsedTimeLabel;
    [SerializeField] private TMP_Text maxPreyLabel;
    [SerializeField] private TMP_Text maxPredatorLabel;

    [Header("Buttons")]
    [SerializeField] private Button quitButton;
    [SerializeField] private Button editButton;
    [SerializeField] private Button restartButton;

    private MapData _lastMap;

    // Si collega all'evento di estinzione lanciato da SimulationSession
    protected override void Awake()
    {
        SimulationSession.OnExtinctionEvent += Show;

        base.Awake();
        quitButton.onClick.AddListener(OnQuit);
        editButton.onClick.AddListener(OnEdit);
        restartButton.onClick.AddListener(OnRestart);
    }

    private void OnDestroy()
    {
        SimulationSession.OnExtinctionEvent -= Show;
    }

    // Mostra le statistiche della simulazione
    private void Show(float elapsed, int maxPrey, int maxPred)
    {
        _lastMap = MapSession.Instance.CurrentMap;

        int s = Mathf.FloorToInt(elapsed);
        elapsedTimeLabel.text = $"Simulation time: {s / 60}:{s % 60:00}";
        maxPreyLabel.text = $"Peak prey population: {maxPrey}";
        maxPredatorLabel.text = $"Peak predator population: {maxPred}";

        OpenPopup();
    }

    private void OnQuit()
    {
        ClosePopup();
        SimulationSession.Instance.Stop();

        // MapSelection con MainScreen nella history
        UIManager.Instance.NavigateClean<MapSelectionScreen>(
            UIManager.Instance.GetScreen<MainScreen>()
        );
    }

    private void OnEdit()
    {
        ClosePopup();
        SimulationSession.Instance.Stop();

        if (_lastMap == null) { UIManager.Instance.NavigateClean<MapSelectionScreen>(); return; }

        var hud = UIManager.Instance.GetScreen<EditorHUD>();
        hud.PrepareForMap(_lastMap);

        // EditorHUD con [MainScreen, MapSelectionScreen] nella history
        UIManager.Instance.NavigateClean<EditorHUD>(
            UIManager.Instance.GetScreen<MainScreen>(),
            UIManager.Instance.GetScreen<MapSelectionScreen>()
        );
    }

    private void OnRestart()
    {
        ClosePopup();
        if (_lastMap == null) return;

        WorldSession.Instance.EnterSimulation(_lastMap);
        SimulationSession.Instance.Begin(_lastMap, SimulationSession.Instance.LastLogEnabled);

        // SimulationHUD con [MainScreen, MapSelectionScreen] nella history
        UIManager.Instance.NavigateClean<SimulationHUD>(
            UIManager.Instance.GetScreen<MainScreen>(),
            UIManager.Instance.GetScreen<MapSelectionScreen>()
        );
    }
}