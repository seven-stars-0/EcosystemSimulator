using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderParam : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Slider   slider;
    [SerializeField] private TMP_Text valueLabel;

    // ── API float ─────────────────────────────────────────────────────────────

    public void Setup(string lbl, float value, float min, float max, Action<float> onChange)
    {
        label.text = lbl;
        slider.onValueChanged.RemoveAllListeners();
        slider.wholeNumbers = false;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = value;
        UpdateFloatLabel(value);
        slider.onValueChanged.AddListener(v => { UpdateFloatLabel(v); onChange?.Invoke(v); });
    }

    // ── API int (nessun decimale) ─────────────────────────────────────────────

    public void SetupInt(string lbl, int value, int min, int max, Action<int> onChange)
    {
        label.text = lbl;
        slider.onValueChanged.RemoveAllListeners();
        slider.wholeNumbers = true;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = value;
        valueLabel.text = value.ToString();
        slider.onValueChanged.AddListener(v =>
        {
            int iv = Mathf.RoundToInt(v);
            valueLabel.text = iv.ToString();
            onChange?.Invoke(iv);
        });
    }

    private void UpdateFloatLabel(float v)
        => valueLabel.text = v < 1f ? v.ToString("F3") : v.ToString("F2");
}