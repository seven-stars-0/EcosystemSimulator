using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabPanel  : MonoBehaviour
{
    public Button tab;
    public GameObject panel;

    [SerializeField] private Color unactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color activeTabColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    TextMeshProUGUI label = null;

    void Awake()
    {
        label = tab.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Show()
    {
        if (label != null)
        {
            label.color = activeTabColor;
        }
        panel.SetActive(true);
    }

    public void Hide()
    {
        if (label != null)
        {
            label.color = unactiveTabColor;
        }
        panel.SetActive(false);
    }
}
