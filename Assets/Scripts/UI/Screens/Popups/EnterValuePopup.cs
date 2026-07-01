using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Permette di inserire valori all'utente
// Viene usato per dare nome alla mappa quando si salva, e per decidere le dimensioni della mappa
//
// Molto simile agli altri popup, con due callback diversi in base alla modalità in cui viene chiamato
// e con due panel diversi per modalità, mostrati in modo mutualmente esclusivo
public class EnterValuePopup : Popup
{
    [SerializeField] private TMP_Text titleText;

    [Header("String mode")]
    [SerializeField] private GameObject stringPanel;
    [SerializeField] private TMP_InputField stringField;

    [Header("Int mode")]
    [SerializeField] private GameObject intPanel;
    [SerializeField] private SliderParam intSlider;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action<string> _onString;
    private Action<int> _onInt;
    private int _intValue;

    protected override void Awake()
    {
        base.Awake();
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(ClosePopup);
    }

    public void RequestString(string title, string initial, Action<string> onConfirm)
    {
        _onString = onConfirm;
        _onInt = null;

        titleText.text = title;
        stringField.text = initial ?? string.Empty;

        stringPanel.SetActive(true);
        intPanel.SetActive(false);

        OpenPopup();
        stringField.Select();
        stringField.MoveTextEnd(false);
    }

    public void RequestInt(string title, int initial, int min, int max, Action<int> onConfirm)
    {
        _onString = null;
        _onInt = onConfirm;
        _intValue = initial;

        titleText.text = title;
        intSlider.SetupInt("Size", initial, min, max, v => _intValue = v);

        stringPanel.SetActive(false);
        intPanel.SetActive(true);

        OpenPopup();
    }

    private void OnConfirm()
    {
        // Caso string
        if (_onString != null)
        {
            string val = stringField.text.Trim();
            if (string.IsNullOrEmpty(val)) return;  // Non si chiude se il campo è vuoto
            ClosePopup();
            _onString.Invoke(val);
        }
        // Caso int
        else
        {
            ClosePopup();
            _onInt?.Invoke(_intValue);
        }
    }
}