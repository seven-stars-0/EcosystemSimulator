// Assets/Scripts/UI/Widgets/ToolBars/SpawnToolBar.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toolbar per SpawnTool.
/// ToggleGroup seleziona il tipo da spawnare (5 opzioni).
/// EraseToggle attiva la modalità cancellazione.
///
/// Layout prefab (VerticalLayoutGroup):
///   ├─ EraseToggle      (Toggle) — "Cancella" — FUORI dal ToggleGroup
///   └─ SpawnTypeGroup   (ToggleGroup)
///       ├─ PreyToggle      "Preda"
///       ├─ PredatorToggle  "Predatore"
///       ├─ PlantToggle     "Pianta"
///       ├─ TreeToggle      "Albero"
///       └─ RockToggle      "Roccia"
///
/// Nota: EraseToggle deve essere un Toggle SEPARATO dal ToggleGroup,
/// altrimenti la sua attivazione deseleziona quello del tipo corrente.
/// </summary>
public class SpawnToolBar : MonoBehaviour
{
    [Header("Modalità")]
    [SerializeField] private Toggle eraseToggle;
    [SerializeField] private Toggle preyToggle;
    [SerializeField] private Toggle predatorToggle;
    [SerializeField] private Toggle plantToggle;
    [SerializeField] private Toggle treeToggle;
    [SerializeField] private Toggle rockToggle;

    private SpawnTool _tool;

    public void Bind(SpawnTool tool)
    {
        _tool = tool;

        // Rimuovi listener vecchi (Bind può essere chiamata più volte)
        eraseToggle.onValueChanged.RemoveAllListeners();
        preyToggle.onValueChanged.RemoveAllListeners();
        predatorToggle.onValueChanged.RemoveAllListeners();
        plantToggle.onValueChanged.RemoveAllListeners();
        treeToggle.onValueChanged.RemoveAllListeners();
        rockToggle.onValueChanged.RemoveAllListeners();

        // Sincronizza con stato corrente del tool
        eraseToggle.isOn = tool.IsErasing;
        preyToggle.isOn = tool.CurrentSpawnable == SpawnableType.Prey;
        predatorToggle.isOn = tool.CurrentSpawnable == SpawnableType.Predator;
        plantToggle.isOn = tool.CurrentSpawnable == SpawnableType.Plant;
        treeToggle.isOn = tool.CurrentSpawnable == SpawnableType.Tree;
        rockToggle.isOn = tool.CurrentSpawnable == SpawnableType.Rock;

        // Erase toggle: non esclusivo con gli altri
        eraseToggle.onValueChanged.AddListener(v => _tool.IsErasing = v);

        // Tipo spawn: attiva solo quando isOn = true (ToggleGroup garantisce esclusività)
        preyToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Prey; });
        predatorToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Predator; });
        plantToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Plant; });
        treeToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Tree; });
        rockToggle.onValueChanged.AddListener(v => { if (v) _tool.CurrentSpawnable = SpawnableType.Rock; });
    }
}