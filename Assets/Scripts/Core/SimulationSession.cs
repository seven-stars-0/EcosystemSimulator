using System;
using UnityEngine;

public class SimulationSession : MonoBehaviour
{
    public static SimulationSession Instance { get; private set; }

    [SerializeField] private SimulationRunner runner;
    [SerializeField] private PlantManager     plantManager;

    public bool IsRunning { get; private set; }
    public bool LastLogEnabled { get; private set; }

    // Unico evento pubblico: sollevato quando tutti gli animali sono morti.
    // Parametri: (elapsedTime, maxPreyCount, maxPredatorCount)
    public static event Action<float, int, int> OnExtinctionEvent;

    public float TimeScale
    {
        get => runner.timeScale;
        set => runner.timeScale = Mathf.Clamp(value, 0.5f, 10f);
    }

    public bool Paused
    {
        get => runner.paused;
        set => runner.paused = value;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }


    // Chiamato all'avvio della simulazione
    public void Begin(MapData data, bool logEnabled)
    {
        // Interrompe simulazione in corso (se presente)
        if (IsRunning) Stop();

        _extinctionFired = false;   // reset per ogni nuova simulazione
        LastLogEnabled = logEnabled;

        runner.StartSimulation(data, plantManager, logEnabled);
        runner.paused    = false;
        runner.timeScale = 1f;
        IsRunning        = true;
    }

    public void Pause()  => runner.paused = true;
    public void Resume() => runner.paused = false;

    public void Stop()
    {
        runner.StopSimulation();
        IsRunning = false;
        WorldSession.Instance.Exit();
    }

    public void SetCameraFollow(Animal target)
        => WorldSession.Instance.Camera.SetFollowTarget(target?.transform);

    public void TogglePOV()
        => WorldSession.Instance.Camera.TogglePOV();

    public bool IsInPOV => WorldSession.Instance.Camera.IsInPOV;

    private bool _extinctionFired;

    private void Update()
    {
        if (!IsRunning || runner.paused) return;
        if (_extinctionFired)            return;

        // Se tutte le specie si estinguono, la simulazione termina
        if (runner.PreyCount == 0 && runner.PredatorCount == 0)
        {
            _extinctionFired = true;
            IsRunning        = false;
            runner.paused    = true;

            // Serve per comunicare ad ExtinctionPopup i valori da mostrare
            OnExtinctionEvent?.Invoke(
                runner.ElapsedTime,
                runner.MaxPreyCount,
                runner.MaxPredatorCount
            );
        }
    }
}
