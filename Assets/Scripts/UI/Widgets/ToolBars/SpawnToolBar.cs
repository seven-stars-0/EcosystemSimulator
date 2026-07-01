using UnityEngine;
using UnityEngine.UI;

// ToggleGroup seleziona il tipo da spawnare (5 opzioni + Erase mode)
// Le icone di Preda/Predatore mostrano lo SPRITE della skin attualmente selezionata (AnimalSkinManager)
public class SpawnToolBar : MonoBehaviour
{
    [Header("Modalità")]
    [SerializeField] private Toggle eraseToggle;
    [SerializeField] private Toggle preyToggle;
    [SerializeField] private Toggle predatorToggle;
    [SerializeField] private Toggle plantToggle;
    [SerializeField] private Toggle treeToggle;
    [SerializeField] private Toggle rockToggle;

    [Header("Icone skin (sprite selezionato)")]
    [SerializeField] private Image preyIcon;
    [SerializeField] private Image predatorIcon;

    private SpawnTool _tool;

    private void OnEnable()
    {
        AnimalSkinManager.Instance.OnChanged += RefreshIcons;
        RefreshIcons();
    }

    private void OnDisable()
    {
        AnimalSkinManager.Instance.OnChanged -= RefreshIcons;
    }

    private void RefreshIcons()
    {
        var m = AnimalSkinManager.Instance;

        if (preyIcon     != null) preyIcon.sprite     = m.PreySprite;
        if (predatorIcon != null) predatorIcon.sprite = m.PredatorSprite;
    }

    // Binding dei toggle UI a SpawnTool
    public void Bind(SpawnTool tool)
    {
        _tool = tool;

        eraseToggle.onValueChanged.RemoveAllListeners();
        preyToggle.onValueChanged.RemoveAllListeners();
        predatorToggle.onValueChanged.RemoveAllListeners();
        plantToggle.onValueChanged.RemoveAllListeners();
        treeToggle.onValueChanged.RemoveAllListeners();
        rockToggle.onValueChanged.RemoveAllListeners();

        eraseToggle.isOn    = tool.IsErasing;
        preyToggle.isOn     = tool.CurrentSpawnable == SpawnableType.Prey;
        predatorToggle.isOn = tool.CurrentSpawnable == SpawnableType.Predator;
        plantToggle.isOn    = tool.CurrentSpawnable == SpawnableType.Plant;
        treeToggle.isOn     = tool.CurrentSpawnable == SpawnableType.Tree;
        rockToggle.isOn     = tool.CurrentSpawnable == SpawnableType.Rock;

        eraseToggle.onValueChanged.AddListener(v => _tool.IsErasing = v);
        preyToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Prey; });
        predatorToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Predator; });
        plantToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Plant; });
        treeToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Tree; });
        rockToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Rock; });

        RefreshIcons();
    }
}
