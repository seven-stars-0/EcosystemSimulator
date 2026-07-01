using System;
using UnityEngine;

// ============================================================================
//  AnimalSkin / AnimalSkinManager
// ----------------------------------------------------------------------------
//  Lista circolare CONDIVISA di "skin" animali (prefab che mostra uno sprite).
//  Due puntatori distinti (prede / predatori) che NON possono mai coincidere:
//  avanzando, un puntatore "scavalca" l'indice dell'altro.
//
//  Fonte di verita' unica: la leggono WorldBuilder (piazzamento editor),
//  SimulationRunner (spawn simulazione) e la UI (SpawnToolBar, AnimalSkinPanel).
//  Gli indici sono persistiti in AppSettings (come lo skybox).
// ============================================================================

[Serializable]
public class AnimalSkin
{
    public string name;
    public GameObject prefab;
    public Sprite sprite;   // opzionale: se null si prova a estrarlo dal prefab

    public Sprite Icon =>
        sprite != null ? sprite :
        (prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null);
}

public class AnimalSkinManager : MonoBehaviour
{
    public static AnimalSkinManager Instance { get; private set; }

    [Header("Lista circolare condivisa (assegna nell'Inspector)")]
    [SerializeField] private AnimalSkin[] skins;

    [Header("Fallback se la lista e' vuota / prefab null")]
    [SerializeField] private GameObject fallbackPreyPrefab;
    [SerializeField] private GameObject fallbackPredatorPrefab;

    [Header("Diagnostica (loggare ogni cambio in Console)")]
    [SerializeField] private bool logSelection = true;

    /// <summary>Sollevato quando la selezione cambia (UI lo ascolta per aggiornarsi).</summary>
    public event Action OnChanged;

    public int Count => skins?.Length ?? 0;
    public int PreyIndex { get; private set; }
    public int PredatorIndex { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Start: dopo che AppSettingsManage.Awake ha caricato le settings.
    private void Start() => LoadFromSettings();

    private void LoadFromSettings()
    {
        var s = AppSettingsManager.Instance?.Settings;
        PreyIndex     = s != null ? s.preyAnimalIndex     : 0;
        PredatorIndex = s != null ? s.predatorAnimalIndex : 1;
        Normalize();
        OnChanged?.Invoke();
    }

    // Garantisce indici validi e DISTINTI.
    private void Normalize()
    {
        int n = Count;
        if (n == 0) return;
        PreyIndex     = Wrap(PreyIndex, n);
        PredatorIndex = Wrap(PredatorIndex, n);
        if (n > 1 && PreyIndex == PredatorIndex)
            PredatorIndex = Wrap(PreyIndex + 1, n);
    }

    private static int Wrap(int i, int n) => ((i % n) + n) % n;

    // ── Prefab / sprite correnti ──────────────────────────────────────────────

    public GameObject PreyPrefab     => PrefabAt(PreyIndex)     ? PrefabAt(PreyIndex)     : fallbackPreyPrefab;
    public GameObject PredatorPrefab => PrefabAt(PredatorIndex) ? PrefabAt(PredatorIndex) : fallbackPredatorPrefab;
    public Sprite     PreySprite     => SpriteAt(PreyIndex);
    public Sprite     PredatorSprite => SpriteAt(PredatorIndex);

    private GameObject PrefabAt(int i)
        => (skins != null && i >= 0 && i < skins.Length) ? skins[i].prefab : null;
    private Sprite SpriteAt(int i)
        => (skins != null && i >= 0 && i < skins.Length) ? skins[i].Icon : null;

    // ── Navigazione circolare con "scavalcamento" dell'altro puntatore ────────

    public void AdvancePrey(int dir)
    {
        int before = PreyIndex;
        PreyIndex = Step(PreyIndex, PredatorIndex, dir);
        if (logSelection) Debug.Log($"[AnimalSkin] AdvancePrey(dir={dir}): prey {before} -> {PreyIndex}  (pred={PredatorIndex})");
        SaveAndNotify();
    }

    public void AdvancePredator(int dir)
    {
        int before = PredatorIndex;
        PredatorIndex = Step(PredatorIndex, PreyIndex, dir);
        if (logSelection) Debug.Log($"[AnimalSkin] AdvancePredator(dir={dir}): pred {before} -> {PredatorIndex}  (prey={PreyIndex})");
        SaveAndNotify();
    }

    // Avanza index di dir (+1/-1) saltando l'indice 'other'. Circolare su n.
    private int Step(int index, int other, int dir)
    {
        int n = Count;
        if (n <= 1) return index;            // con 0/1 skin non esistono due puntatori distinti
        dir = dir >= 0 ? 1 : -1;
        index = Wrap(index + dir, n);
        if (index == other) index = Wrap(index + dir, n);   // scavalca l'altro
        return index;
    }

    private void SaveAndNotify()
    {
        var s = AppSettingsManager.Instance?.Settings;
        if (s != null)
        {
            s.preyAnimalIndex     = PreyIndex;
            s.predatorAnimalIndex = PredatorIndex;
            AppSettingsManager.Instance.Save();
        }
        OnChanged?.Invoke();
    }
}
