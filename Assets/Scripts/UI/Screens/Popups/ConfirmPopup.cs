using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmPopup : Popup
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action _onConfirm;

    protected override void Awake()
    {
        base.Awake();
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
    }

    public void Present(string message, Action onConfirm)
    {
        _onConfirm = onConfirm;
        messageText.text = message;
        OpenPopup();
    }

    private void OnConfirm()
    {
        ClosePopup();
        _onConfirm?.Invoke();   // invoca DOPO Close: l'azione può aprire altri popup
    }

    private void OnCancel() => ClosePopup();
}