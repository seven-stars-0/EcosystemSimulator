using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIScreen : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.12f;

    private CanvasGroup _cg;
    private Coroutine   _fade;

    // Awake NON chiama più HideImmediate.
    // UIManager.Awake forza l'attivazione di ogni schermata (per far girare questo Awake)
    // e poi chiama HideImmediate su ciascuna.
    protected virtual void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
    }

    // ── Ciclo vita ────────────────────────────────────────────────────────────

    public void Show()
    {
        StopFade();
        gameObject.SetActive(true);
        _cg.alpha          = 1f;
        _cg.interactable   = true;
        _cg.blocksRaycasts = true;
        OnShow();
    }

    public void Hide()
    {
        StopFade();
        _cg.interactable   = false;
        _cg.blocksRaycasts = false;
        _fade = StartCoroutine(FadeAndDisable());
        OnHide();
    }

    public void HideImmediate()
    {
        StopFade();
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

    private IEnumerator FadeAndDisable()
    {
        float t = fadeDuration;
        while (t > 0f)
        {
            t         -= Time.unscaledDeltaTime;
            _cg.alpha  = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        _cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void StopFade()
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
    }
}