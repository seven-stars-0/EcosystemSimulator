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
    private readonly Stack<UIScreen> _history = new(); // Per il back

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

    // Attiva il popup (così gira il suo Awake, che lo nasconde via CanvasGroup) e lo lascia attivo:
    // I popup non vengono mai disattivati, solo resi invisibili.
    private static void ForceInitPopup(Popup popup)
    {
        if (popup == null) return;
        popup.gameObject.SetActive(true);
    }

    private void Start() => Show<MainScreen>(pushToHistory: false);

    // Usato per navigare le schermate
    public T GetScreen<T>() where T : UIScreen
    {
        if (_registry.TryGetValue(typeof(T), out UIScreen s)) return (T)s;
        Debug.LogError($"[UIManager] {typeof(T).Name} not registered.");
        return null;
    }

    // Usato per mostrare la schermata successiva DI CUI SI HA L'ISTANZA
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

    // Usato per mostrare la schermata successiva di cui NON SI HA L'ISTANZA
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

    // Naviga a T ricostruendo la history in modo esplicito
    // Richiede gli schermi genitore dal più vecchio al più recente
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