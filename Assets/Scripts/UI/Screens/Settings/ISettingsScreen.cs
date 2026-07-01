using UnityEngine;
using UnityEngine.UI;

public abstract class ISettingsScreen : UIScreen
{
    [Header("Navigation")]
    [SerializeField] protected Button backBtn;

    [Header("Tab system")]
    [SerializeField] protected TabPanel[] tabPanels;

    protected override void Awake()
    {
        base.Awake();

        backBtn.onClick.AddListener(() => UIManager.Instance.GoBack());

        for (int i = 0; i < tabPanels.Length; i++)
        {
            int captured = i;
            tabPanels[i].tab.onClick.AddListener(() => ShowTab(captured));
        }
    }

    protected override void OnShow()
    {
        InputGuard.CameraInputBlocked = true;
        OnBind();
        ShowTab(0);
    }

    protected override void OnHide()
    {
        InputGuard.CameraInputBlocked = false;
        OnUnbind();
    }

    // Template method per le sottoclassi

    // Chiamato appena prima che la schermata diventi visibile.
    // Le sottoclassi inizializzano/aggiornano i propri panel qui
    protected virtual void OnBind() { }

    // Chiamato quando la schermata viene nascosta.
    // Le sottoclassi possono persistere modifiche, segnare dirty flag, ecc.
    protected virtual void OnUnbind() { }


    // Mostra la tab premuta
    protected void ShowTab(int index)
    {
        for (int j = 0; j < tabPanels.Length; j++)
            tabPanels[j].Hide();
        if (index >= 0 && index < tabPanels.Length)
            tabPanels[index].Show();
    }
}