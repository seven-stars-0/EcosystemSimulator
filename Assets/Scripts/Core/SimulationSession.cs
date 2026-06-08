using System;
using UnityEngine;
 
public class SimulationSession : MonoBehaviour
{
    public static SimulationSession Instance { get; private set; }
 
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private PlantManager     plantManager;
 
    public bool IsRunning { get; private set; }
 
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
 
    // ── API pubblica ──────────────────────────────────────────────────────────
 
    public void Begin(MapData data)
    {
        if (IsRunning) Stop();
 
        _extinctionFired = false;   // FIX: reset per ogni nuova simulazione
 
        runner.StartSimulation(data, plantManager);
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
 
    // ── Rilevamento estinzione (polling) ──────────────────────────────────────
 
    private const float ExtinctionGraceTime = 5f;
    private bool        _extinctionFired;
 
    private void Update()
    {
        if (!IsRunning || runner.paused) return;
        if (_extinctionFired)            return;
        if (runner.ElapsedTime < ExtinctionGraceTime) return;
 
        if (runner.PreyCount == 0 && runner.PredatorCount == 0)
        {
            _extinctionFired = true;
            IsRunning        = false;
            runner.paused    = true;
 
            OnExtinctionEvent?.Invoke(
                runner.ElapsedTime,
                runner.MaxPreyCount,
                runner.MaxPredatorCount
            );
        }
    }
}