using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    [SerializeField] private UIScreen[] screens;

    [Header("Popups")]
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private EnterValuePopup enterValuePopup;
    [SerializeField] private ExtinctionPopup extinctionPopup;

    private readonly Dictionary<Type, UIScreen> _registry = new();
    private UIScreen _current;
    private readonly Stack<UIScreen> _history = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var s in screens)
        {
            if (s == null) continue;
            _registry[s.GetType()] = s;
            s.gameObject.SetActive(true);
            s.HideImmediate();
        }

        ForceInitPopup(confirmPopup);
        ForceInitPopup(enterValuePopup);
        ForceInitPopup(extinctionPopup);
    }

    /// <summary>
    /// Garantisce che il popup abbia girato il suo Awake (attivandolo brevemente
    /// se era inattivo) e lo lascia SEMPRE disattivato alla fine.
    ///
    /// Il bug precedente: se il popup partiva attivo in Inspector, Awake girava
    /// subito e lo disattivava. ForceInitPopup lo riattivava (Awake non ri-gira)
    /// e non lo ri-disattivava → popup rimasto visibile.
    /// Fix: SetActive(false) incondizionato alla fine.
    /// </summary>
    private static void ForceInitPopup(Popup popup)
    {
        if (popup == null) return;

        // Se inattivo, attivalo per triggerare Awake (una tantum)
        if (!popup.gameObject.activeSelf)
            popup.gameObject.SetActive(true);

        // Garantisce sempre lo stato nascosto, qualunque cosa sia successa
        popup.gameObject.SetActive(false);
    }

    private void Start() => Show<MainScreen>(pushToHistory: false);

    public T GetScreen<T>() where T : UIScreen
    {
        if (_registry.TryGetValue(typeof(T), out UIScreen s)) return (T)s;
        Debug.LogError($"[UIManager] {typeof(T).Name} not registered.");
        return null;
    }

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

    public void GoBack()
    {
        if (_history.Count == 0) { Debug.LogWarning("[UIManager] GoBack: history empty."); return; }
        _current.Hide();
        _current = _history.Pop();
        _current.Show();
    }

    public void GoToMain()
    {
        _current?.HideImmediate();
        _history.Clear();
        _current = GetScreen<MainScreen>();
        _current?.Show();
    }

    /// <summary>
    /// Naviga a T ricostruendo la history in modo esplicito.
    /// Passare gli schermi genitore dal più vecchio al più recente.
    /// Esempio: NavigateClean&lt;EditorHUD&gt;(GetScreen&lt;MainScreen&gt;(), GetScreen&lt;MapSelectionScreen&gt;())
    /// </summary>
    public void NavigateClean<T>(params UIScreen[] historyFromOldestToNewest) where T : UIScreen
    {
        _current?.HideImmediate();
        _history.Clear();
        foreach (var screen in historyFromOldestToNewest)
            if (screen != null) _history.Push(screen);
        _current = GetScreen<T>();
        _current?.Show();
    }

    public void ShowConfirm(string message, Action onConfirm)
        => confirmPopup.Present(message, onConfirm);

    public void RequestString(string title, string initial, Action<string> onConfirm)
        => enterValuePopup.RequestString(title, initial, onConfirm);

    public void RequestInt(string title, int initial, int min, int max, Action<int> onConfirm)
        => enterValuePopup.RequestInt(title, initial, min, max, onConfirm);
}