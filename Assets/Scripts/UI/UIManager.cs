using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens — drag all UIScreens here")]
    [SerializeField] private UIScreen[] screens;

    [Header("Popups — drag all PopupBase here")]
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private EnterValuePopup enterValuePopup;
    [SerializeField] private ExtinctionPopup extinctionPopup;

    private readonly Dictionary<Type, UIScreen> _registry = new();
    private UIScreen _current;
    private readonly Stack<UIScreen> _history = new();

    // ── Init ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Registra e nasconde tutte le schermate.
        // SetActive(true) garantisce che Awake sia già girato prima di HideImmediate.
        foreach (var s in screens)
        {
            if (s == null) continue;
            _registry[s.GetType()] = s;
            s.gameObject.SetActive(true);
            s.HideImmediate();
        }

        // Forza l'Awake dei popup prima del primo utilizzo.
        // PopupBase.Awake fa SetActive(false), quindi dopo questo
        // ogni popup è inizializzato e nascosto.
        ForceInitPopup(confirmPopup);
        ForceInitPopup(enterValuePopup);
        ForceInitPopup(extinctionPopup);
    }

    /// <summary>
    /// SetActive(true) triggerà Awake del popup se non è ancora girato.
    /// PopupBase.Awake chiama SetActive(false): il popup si auto-nasconde.
    /// Serve solo se il popup parte inattivo nell'Inspector.
    /// </summary>
    private static void ForceInitPopup(Popup popup)
    {
        if (popup == null) return;
        if (popup.gameObject.activeSelf) return;  // Awake già girato
        popup.gameObject.SetActive(true);          // triggerà Awake → SetActive(false)
    }

    private void Start() => Show<MainScreen>(pushToHistory: false);

    // ── Registry ──────────────────────────────────────────────────────────────

    public T GetScreen<T>() where T : UIScreen
    {
        if (_registry.TryGetValue(typeof(T), out UIScreen s)) return (T)s;
        Debug.LogError($"[UIManager] {typeof(T).Name} not registered in Inspector.");
        return null;
    }

    // ── Navigazione standard ──────────────────────────────────────────────────

    /// <summary>
    /// Mostra una schermata. La schermata corrente viene nascosta con fade.
    /// Se pushToHistory=true, la schermata corrente va nella history
    /// (Back la riattiverà).
    /// </summary>
    public void Show(UIScreen next, bool pushToHistory = true)
    {
        if (next == null) { Debug.LogError("[UIManager] Show called with null screen."); return; }

        if (_current != null)
        {
            if (pushToHistory) _history.Push(_current);
            _current.Hide();
        }
        _current = next;
        _current.Show();
    }

    public void Show<T>(bool pushToHistory = true) where T : UIScreen
    {
        var s = GetScreen<T>();
        if (s != null) Show(s, pushToHistory);
    }

    /// <summary>
    /// Torna alla schermata precedente nello stack.
    /// </summary>
    public void GoBack()
    {
        if (_history.Count == 0)
        {
            Debug.LogWarning("[UIManager] GoBack called but history is empty.");
            return;
        }

        _current.Hide();
        _current = _history.Pop();
        _current.Show();
    }

    /// <summary>
    /// Torna alla MainScreen con history vuota.
    /// </summary>
    public void GoToMain()
    {
        _current?.HideImmediate();
        _history.Clear();
        _current = GetScreen<MainScreen>();
        _current?.Show();
    }

    // ── Navigazione pulita (per uscire da simulazione/estinzione) ────────────

    /// <summary>
    /// Naviga direttamente a <typeparamref name="T"/> ricostruendo la history
    /// in modo esplicito dai parametri (dal più vecchio al più recente).
    ///
    /// Esempio — EditorHUD con Back funzionante:
    ///   NavigateClean&lt;EditorHUD&gt;(GetScreen&lt;MainScreen&gt;(), GetScreen&lt;MapSelectionScreen&gt;())
    ///   → history: [MainScreen, MapSelectionScreen]
    ///   → Back da EditorHUD → MapSelectionScreen → Back → MainScreen ✓
    ///
    /// Esempio — MapSelectionScreen con Back verso MainScreen:
    ///   NavigateClean&lt;MapSelectionScreen&gt;(GetScreen&lt;MainScreen&gt;())
    ///   → history: [MainScreen]
    ///   → Back da MapSelectionScreen → MainScreen ✓
    /// </summary>
    public void NavigateClean<T>(params UIScreen[] historyFromOldestToNewest) where T : UIScreen
    {
        _current?.HideImmediate();
        _history.Clear();

        // Push dal più vecchio al più recente: Pop restituirà il più recente per primo
        foreach (var screen in historyFromOldestToNewest)
            if (screen != null) _history.Push(screen);

        _current = GetScreen<T>();
        _current?.Show();
    }

    // ── Popup API ─────────────────────────────────────────────────────────────

    public void ShowConfirm(string message, Action onConfirm)
        => confirmPopup.Present(message, onConfirm);

    public void RequestString(string title, string initial, Action<string> onConfirm)
        => enterValuePopup.RequestString(title, initial, onConfirm);

    public void RequestInt(string title, int initial, int min, int max, Action<int> onConfirm)
        => enterValuePopup.RequestInt(title, initial, min, max, onConfirm);
}