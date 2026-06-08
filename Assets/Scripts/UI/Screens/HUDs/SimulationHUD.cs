using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SimulationHUD : UIScreen
{
    [Header("Controlli")]
    [SerializeField] private Button stopButton;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private SliderParam speedSlider;

    [Header("Colori pausa")]
    [SerializeField] private Color pauseColor = new Color(0.85f, 0.64f, 0.12f, 1f);
    [SerializeField] private Color resumeColor = new Color(0.60f, 0.80f, 0.19f, 1f);

    [Header("Statistiche")]
    [SerializeField] private TMP_Text preyCountLabel;
    [SerializeField] private TMP_Text predCountLabel;
    [SerializeField] private TMP_Text plantCountLabel;
    [SerializeField] private TMP_Text elapsedTimeLabel;
    [SerializeField] private TMP_Text pauseResumeLabel;

    [Header("Camera follow")]
    [SerializeField] private Button unlockCameraButton;

    [Header("POV")]
    [SerializeField] private Button povButton;
    [SerializeField] private TMP_Text povButtonLabel;

    [Header("Colori POV")]
    [SerializeField] private Color povActiveColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color povInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private SimulationSession Sim => SimulationSession.Instance;
    private SimulationRunner Runner => SimulationRunner.Instance;

    private Animal _followedAnimal;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        stopButton.onClick.AddListener(OnStop);
        pauseResumeButton.onClick.AddListener(OnPauseResume);
        unlockCameraButton.onClick.AddListener(OnUnlockCamera);
        povButton.onClick.AddListener(OnTogglePOV);
        speedSlider.Setup("Speed", 1f, 0.5f, 10f, v => Sim.TimeScale = v);
    }

    protected override void OnShow()
    {
        InputGuard.CameraInputBlocked = false;
        speedSlider.Setup("Speed", Sim.TimeScale, 0.5f, 10f, v => Sim.TimeScale = v);
        UpdatePauseLabel();
        SetFollowUI(false);
    }

    protected override void OnHide()
    {
        if (Sim != null && Sim.IsInPOV) Sim.TogglePOV();
        StopFollowing();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!gameObject.activeSelf || Runner == null) return;

        UpdateStats();
        CheckFollowedAnimalAlive();

        if (!Sim.IsInPOV)
            HandleCameraClickInput();

        if (_followedAnimal != null && Keyboard.current.fKey.wasPressedThisFrame)
            OnTogglePOV();
    }

    private void UpdateStats()
    {
        preyCountLabel.text = $"Prey: {Runner.PreyCount}";
        predCountLabel.text = $"Predators: {Runner.PredatorCount}";
        plantCountLabel.text = $"Plants: {Runner.PlantCount}";
        int s = Mathf.FloorToInt(Runner.ElapsedTime);
        elapsedTimeLabel.text = $"Time: {s / 60}:{s % 60:00}";
    }

    // ── Camera follow ─────────────────────────────────────────────────────────

    private void CheckFollowedAnimalAlive()
    {
        if (_followedAnimal == null) return;
        if (!_followedAnimal.IsAlive)
        {
            if (Sim.IsInPOV) Sim.TogglePOV();
            StopFollowing();
        }
    }

    private void HandleCameraClickInput()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            var animal = hit.collider.GetComponentInParent<Animal>();
            if (animal != null && animal.IsAlive) { StartFollowing(animal); return; }
        }

        if (_followedAnimal != null) StopFollowing();
    }

    private void StartFollowing(Animal target)
    {
        if (Sim.IsInPOV) Sim.TogglePOV();
        _followedAnimal = target;
        Sim.SetCameraFollow(target);
        SetFollowUI(true);
    }

    private void StopFollowing()
    {
        _followedAnimal = null;
        Sim.SetCameraFollow(null);
        SetFollowUI(false);
    }

    private void OnUnlockCamera()
    {
        if (Sim.IsInPOV) Sim.TogglePOV();
        StopFollowing();
    }

    // ── POV ───────────────────────────────────────────────────────────────────

    private void OnTogglePOV()
    {
        if (_followedAnimal == null) return;
        Sim.TogglePOV();
        UpdatePOVButton();
    }

    private void UpdatePOVButton()
    {
        bool active = Sim.IsInPOV;
        povButtonLabel.text = active ? "EXIT POV" : "POV";
        povButtonLabel.color = active ? povActiveColor : povInactiveColor;
    }

    private void SetFollowUI(bool active)
    {
        unlockCameraButton.gameObject.SetActive(active);
        povButton.gameObject.SetActive(active);
        if (!active) UpdatePOVButton();
    }

    // ── Stop / Pause ──────────────────────────────────────────────────────────

    private void OnStop()
    {
        bool wasPaused = Sim.Paused;
        Sim.Paused = true;

        UIManager.Instance.ShowConfirm(
            "Stop the simulation?\nAll progress will be lost.",
            onConfirm: () =>
            {
                if (Sim.IsInPOV) Sim.TogglePOV();
                StopFollowing();
                Sim.Stop();

                // MapSelection con MainScreen nella history: Back funziona
                UIManager.Instance.NavigateClean<MapSelectionScreen>(
                    UIManager.Instance.GetScreen<MainScreen>()
                );
            }
        );

        if (!wasPaused)
            StartCoroutine(ResumeIfNotStopped());
    }

    private IEnumerator ResumeIfNotStopped()
    {
        yield return new WaitForSecondsRealtime(0.15f);
        if (Sim != null && Sim.IsRunning) Sim.Paused = false;
    }

    private void OnPauseResume()
    {
        Sim.Paused = !Sim.Paused;
        UpdatePauseLabel();
    }

    private void UpdatePauseLabel()
    {
        bool p = Sim.Paused;
        pauseResumeLabel.text = p ? "RESUME" : "PAUSE";
        pauseResumeLabel.color = p ? resumeColor : pauseColor;
    }
}