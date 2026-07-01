using UnityEngine;

// IMPORTANTE: I popup non vengono mai disattivati, ma diventano solo invisibili e non interagibili
// Questo serve perché alcuni popup appaiono premendo bottoni, e se fossero disattivi bisognerebbe premere il bottone due volte per farli comparire
// Se sembra strano, beh, lo è. Ma fare questa cosa ha risolto il problema
[RequireComponent(typeof(CanvasGroup))]
public abstract class Popup : MonoBehaviour
{
    protected CanvasGroup Cg { get; private set; }

    protected virtual void Awake()
    {
        Cg = GetComponent<CanvasGroup>();
        HideImmediate();   // nascosto via CanvasGroup, oggetto attivo
    }

    protected void OpenPopup()
    {
        gameObject.SetActive(true);        // safety se qualcuno l'avesse disattivato
        transform.SetAsLastSibling();      // Si disegna sopra tutto
        Cg.alpha = 1f;
        Cg.interactable = true;
        Cg.blocksRaycasts = true;
        InputGuard.CameraInputBlocked = true;
    }

    protected void ClosePopup()
    {
        Cg.alpha = 0f;
        Cg.interactable = false;
        Cg.blocksRaycasts = false;
        InputGuard.CameraInputBlocked = false;
    }

    private void HideImmediate()
    {
        Cg.alpha = 0f;
        Cg.interactable = false;
        Cg.blocksRaycasts = false;
    }
}
