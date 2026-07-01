using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIScreen : MonoBehaviour
{
    private CanvasGroup _cg;

    protected virtual void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
    }

    // Attiva il GO e rende lo schermo visibile (alpha = 1), interactable e che blocca i raycast
    public void Show()
    {
        gameObject.SetActive(true);
        _cg.alpha          = 1f;
        _cg.interactable   = true;
        _cg.blocksRaycasts = true;
        OnShow();
    }

    // Operazione inversa di Show
    public void Hide()
    {
        _cg.alpha          = 0f;   // <-- senza questo la schermata resta DIPINTA sotto la successiva
        _cg.interactable   = false;
        _cg.blocksRaycasts = false;
        gameObject.SetActive(false);
        OnHide();
    }

    public void HideImmediate()
    {
        if (_cg == null) _cg = GetComponent<CanvasGroup>(); // guard per Awake tardivo
        _cg.alpha          = 0f;
        _cg.interactable   = false;
        _cg.blocksRaycasts = false;
        gameObject.SetActive(false);
        OnHide();
    }

    public void SetBlocksRaycasts(bool v) => _cg.blocksRaycasts = v;

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}