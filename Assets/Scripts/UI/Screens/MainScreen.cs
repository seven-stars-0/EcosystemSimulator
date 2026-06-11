using UnityEngine;
using UnityEngine.UI;

public class MainScreen : UIScreen
{
    [SerializeField] private Button mapsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    protected override void Awake()
    {
        base.Awake();   // già chiama HideImmediate — NO gameObject.SetActive ridondante
        mapsButton.onClick.AddListener(() => UIManager.Instance.Show<MapSelectionScreen>());
        settingsButton.onClick.AddListener(() => UIManager.Instance.Show<GeneralSettingsScreen>());
        quitButton.onClick.AddListener(() => Application.Quit());

        Debug.Log("SCRIPT MODIFICATO");
    }
}