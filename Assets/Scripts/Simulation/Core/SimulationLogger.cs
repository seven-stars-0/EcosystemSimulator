using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public class SimulationLogger
{
    private float _sampleInterval;
    private StreamWriter _writer;
    private string _path;
    private float _nextSampleTime;
    private bool _active;

    public string FilePath => _path;
    public bool IsActive => _active;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public SimulationLogger(float sampleIntervalSeconds = 1f)
    {
        _sampleInterval = Mathf.Max(0.1f, sampleIntervalSeconds);
    }

    public void Begin(SimulationSettings s, int prey0, int predators0, int plants0)
    {
        Close();   // chiude un eventuale log precedente
        _sampleInterval = Mathf.Clamp(s.logSampleInterval, 1f, 120f);   // intervallo scelto dall'utente
        try
        {
            // Stesso schema del salvataggio mappe (MapSaveManager): in editor i
            // log stanno nel progetto, nella build nello storage persistente.
#if UNITY_EDITOR
            string dir = Path.Combine(Application.dataPath, "SimulationLogs");
#else
            string dir = Path.Combine(Application.persistentDataPath, "SimulationLogs");
#endif
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"sim_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            _writer = new StreamWriter(_path, false, new UTF8Encoding(false));

            WriteLine("# EcoSim run log");
            WriteLine($"# timestamp={DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Inv)}");
            WriteLine($"# sampleIntervalSeconds={Num(_sampleInterval)}");
            WriteLine("#");

            WriteLine("# --- SimulationSettings ---");
            foreach (FieldInfo f in typeof(SimulationSettings)
                         .GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v = f.GetValue(s);
                string vs = v is float fl ? Num(fl) : Convert.ToString(v, Inv);
                WriteLine($"# {f.Name}={vs}");
            }
            WriteLine("#");

            WriteLine("# --- Initial Population ---");
            WriteLine($"# prey0={prey0}");
            WriteLine($"# predators0={predators0}");
            WriteLine($"# plants0={plants0}");
            WriteLine("#");

            WriteLine("t_seconds,prey,predators,plants");
            _writer.Flush();

            _active = true;
            Sample(0f, prey0, predators0, plants0);   // riga a t=0
            _nextSampleTime = _sampleInterval;

            Debug.Log($"[SimulationLogger] Logging on: {_path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SimulationLogger] Couldn't open log file: {e.Message}");
            _active = false;
        }
    }

    // Chiamato ogni frame da SimulationRunner. Emette una riga per ogni soglia temporale superata
    public void Tick(float elapsed, int prey, int predators, int plants)
    {
        if (!_active) return;
        while (elapsed + 1e-4f >= _nextSampleTime)
        {
            Sample(_nextSampleTime, prey, predators, plants);
            _nextSampleTime += _sampleInterval;
        }
    }

    public void End(int peakPrey, int peakPredators, float elapsed)
    {
        if (!_active) return;
        WriteLine("#");
        WriteLine("# --- Riepilogo ---");
        WriteLine($"# duration_s={Num(elapsed)}");
        WriteLine($"# peakPrey={peakPrey}");
        WriteLine($"# peakPredators={peakPredators}");
        Close();
    }

    public void Close()
    {
        try { _writer?.Flush(); _writer?.Dispose(); } catch { /* ignore */ }
        _writer = null;
        _active = false;
    }

    private void Sample(float t, int prey, int predators, int plants)
    {
        if (_writer == null) return;
        _writer.WriteLine($"{t.ToString("0.0", Inv)},{prey},{predators},{plants}");
        _writer.Flush();
    }

    private void WriteLine(string line) => _writer?.WriteLine(line);
    private static string Num(float v) => v.ToString("0.######", Inv);
}
