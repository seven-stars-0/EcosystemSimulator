// Assets/Scripts/UI/Widgets/ToolBars/TerrainToolBar.cs
using UnityEngine;
using UnityEngine.UI;

public class TerrainToolBar : MonoBehaviour
{
    [SerializeField] private SliderParam strengthSlider;
    [SerializeField] private SliderParam radiusSlider;
    [SerializeField] private Toggle raiseToggle;
    [SerializeField] private Toggle smoothToggle;

    private TerrainTool _tool;

    public void Bind(TerrainTool tool)
    {
        _tool = tool;

        strengthSlider.Setup("Strength", tool.Strength, 0.01f, 1f, v => _tool.Strength = v);
        radiusSlider.Setup("Radius", tool.Radius, 1f, 10f, v => _tool.Radius = Mathf.RoundToInt(v));

        // FIX: RemoveAllListeners PRIMA di settare isOn
        raiseToggle.onValueChanged.RemoveAllListeners();
        smoothToggle.onValueChanged.RemoveAllListeners();

        raiseToggle.isOn = tool.Raise;
        smoothToggle.isOn = tool.Smooth;

        raiseToggle.onValueChanged.AddListener(v => _tool.Raise = v);
        smoothToggle.onValueChanged.AddListener(v => _tool.Smooth = v);
    }
}