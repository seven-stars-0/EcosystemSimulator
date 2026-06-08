using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class Popup : MonoBehaviour
{
    protected CanvasGroup Cg { get; private set; }

    protected virtual void Awake()
    {
        Cg = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    protected void OpenPopup()
    {
        gameObject.SetActive(true);

        // Diventa l'ultimo figlio del Canvas → si disegna sopra tutto
        transform.SetAsLastSibling();

        Cg.alpha = 1f;
        Cg.interactable = true;
        Cg.blocksRaycasts = true;
        InputGuard.CameraInputBlocked = true;
    }

    protected void ClosePopup()
    {
        Cg.interactable = false;
        Cg.blocksRaycasts = false;
        gameObject.SetActive(false);
        InputGuard.CameraInputBlocked = false;
    }
}