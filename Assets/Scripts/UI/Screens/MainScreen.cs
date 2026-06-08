using UnityEngine;
using UnityEngine.UI;

public class MainScreen : UIScreen
{
    [SerializeField] private Button exploreButton;
    [SerializeField] private Button quitButton;

    protected override void Awake()
    {
        base.Awake();   // già chiama HideImmediate — NO gameObject.SetActive ridondante
        exploreButton.onClick.AddListener(() => UIManager.Instance.Show<MapSelectionScreen>());
        quitButton.onClick.AddListener(() => Application.Quit());
    }
}