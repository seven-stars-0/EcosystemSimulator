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
    [SerializeField] private TMP_Text pauseResumeLabel;

    [Header("Grafico popolazioni")]
    [SerializeField] private Button toggleGraphButton;
    [SerializeField] private PopulationGraphHUD populationGraph;

    [Header("Colori pausa")]
    [SerializeField] private Color pauseColor = new Color(0.85f, 0.64f, 0.12f, 1f);
    [SerializeField] private Color resumeColor = new Color(0.60f, 0.80f, 0.19f, 1f);

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
    private bool   _isFollowing;

    // Setup dei listener e SliderParam
    protected override void Awake()
    {
        base.Awake();

        stopButton.onClick.AddListener(OnStop);
        pauseResumeButton.onClick.AddListener(OnPauseResume);
        unlockCameraButton.onClick.AddListener(OnUnlockCamera);
        povButton.onClick.AddListener(OnTogglePOV);
        toggleGraphButton.onClick.AddListener(OnToggleGraph);
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

    private void Update()
    {
        if (!gameObject.activeSelf || Runner == null) return;

        // Verifichiamo che l'animale in follow esista ancora
        CheckFollowedAnimalAlive();

        // Esegue raycast se l'utente clicca, entrando in modalità follow se si preme su un animale
        if (!Sim.IsInPOV)
            HandleCameraClickInput();

        // Shortcut per modalità POV (premere F)
        if (_followedAnimal != null && Keyboard.current.fKey.wasPressedThisFrame)
            OnTogglePOV();
    }

    private void CheckFollowedAnimalAlive()
    {
        // Se non stiamo seguendo nessuno, pazienza
        if (!_isFollowing) return;

        // Se l'animale muore o smette di esistere, interrompiamo il follow
        if (_followedAnimal == null || !_followedAnimal.IsAlive)
        {
            if (Sim.IsInPOV) Sim.TogglePOV();
            StopFollowing();
        }
    }

    // Gestisce l'entrata e l'uscita della camera in modalità Follow tramite raycast
    private void HandleCameraClickInput()
    {
        // Se l'utente non clicca, oppure clicca sopra un componente UI, allora nulla
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Partendo dalla camera, facciamo un raycast
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Se colpiamo un animale, la Camera passa in modalità Follow
            var animal = hit.collider.GetComponentInParent<Animal>();
            if (animal != null && animal.IsAlive) { StartFollowing(animal); return; }
        }

        // Se l'utente non preme su un animale, allora esce dalla modalità follow
        if (_followedAnimal != null) StopFollowing();
    }

    private void StartFollowing(Animal target)
    {
        if (Sim.IsInPOV) Sim.TogglePOV();
        _followedAnimal = target;
        _isFollowing = true;

        Sim.SetCameraFollow(target);
        SetFollowUI(true);
    }

    private void StopFollowing()
    {
        _followedAnimal = null;
        _isFollowing = false;

        Sim.SetCameraFollow(null);
        SetFollowUI(false);
    }

    // Metodo chiamato dal bottone STOP FOLLOWING
    private void OnUnlockCamera()
    {
        if (Sim.IsInPOV) Sim.TogglePOV();
        StopFollowing();
    }

    private void OnTogglePOV()
    {
        if (_followedAnimal == null) return;
        Sim.TogglePOV();
        UpdatePOVButton();
    }

    // Cambia la label del POV button
    private void UpdatePOVButton()
    {
        bool active = Sim.IsInPOV;
        povButtonLabel.text = active ? "EXIT POV" : "POV";
        povButtonLabel.color = active ? povActiveColor : povInactiveColor;
    }

    // I bottoni STOP FOLLOWING e POV vengono attivati solo se si entra in modalità follow
    private void SetFollowUI(bool active)
    {
        unlockCameraButton.gameObject.SetActive(active);
        povButton.gameObject.SetActive(active);
        if (!active) UpdatePOVButton();
    }

    // Mette in pausa la simulazione, chiede conferma all'utente, e in caso positivo riporta l'utente in MapSelectionScreen
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

    private void OnToggleGraph()
    {
        if (populationGraph != null) populationGraph.ToggleChart();
    }

    private void UpdatePauseLabel()
    {
        bool p = Sim.Paused;
        pauseResumeLabel.text = p ? "RESUME" : "PAUSE";
        pauseResumeLabel.color = p ? resumeColor : pauseColor;
    }
}
