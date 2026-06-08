using UnityEngine;
using UnityEngine.UI;
 
public class FertilityPanel : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private RawImage fertilityMap;
 
    [Header("Colors")]
    [SerializeField] private Color zeroColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    [SerializeField] private Color fullColor = new Color(0.20f, 0.72f, 0.15f, 1f);
 
    [Header("Noise controls")]
    [SerializeField] private SliderParam noiseScaleSlider;
    [SerializeField] private SliderParam noiseStrengthSlider;
    [SerializeField] private Button      applyNoiseButton;
 
    [Header("Radial controls")]
    [SerializeField] private SliderParam radialRadiusSlider;
    [SerializeField] private SliderParam radialStrengthSlider;
    [SerializeField] private Button      applyRadialButton;
 
    [Header("Reset")]
    [SerializeField] private Button resetButton;
 
    private WorldGrid _grid;
    private Texture2D _tex;
 
    private float _noiseScale     = 0.08f;
    private float _noiseStrength  = 0.6f;
    private float _radialRadius   = 20f;
    private float _radialStrength = 0.8f;
 
    private void Awake()
    {
        noiseScaleSlider    .Setup("Noise scale",     _noiseScale,     0.01f, 0.5f, v => _noiseScale     = v);
        noiseStrengthSlider .Setup("Intensity",       _noiseStrength,  0f,    1f,   v => _noiseStrength   = v);
        radialRadiusSlider  .Setup("Radius",          _radialRadius,   1f,    64f,  v => _radialRadius    = v);
        radialStrengthSlider.Setup("Intensity",       _radialStrength, -1f,   1f,   v => _radialStrength  = v);
 
        applyNoiseButton .onClick.AddListener(ApplyNoise);
        applyRadialButton.onClick.AddListener(ApplyRadial);
        resetButton      .onClick.AddListener(Reset);
    }
 
    public void Bind(WorldGrid grid)
    {
        _grid = grid;
        RebuildTexture();
    }
 
    private void ApplyNoise()
    {
        if (_grid == null) return;
        _grid.ApplyFertilityNoise(_noiseScale, _noiseStrength,
            Random.Range(0f, 1000f), Random.Range(0f, 1000f));
        RebuildTexture();
        MapSession.Instance?.MarkDirty();
    }
 
    private void ApplyRadial()
    {
        if (_grid == null) return;
        _grid.ApplyFertilityRadial(_grid.size * 0.5f, _grid.size * 0.5f,
            _radialRadius, _radialStrength);
        RebuildTexture();
        MapSession.Instance?.MarkDirty();
    }
 
    private void Reset()
    {
        if (_grid == null) return;
        _grid.SetFertilityUniform(0.5f);
        RebuildTexture();
        MapSession.Instance?.MarkDirty();
    }
 
    private void RebuildTexture()
    {
        if (_grid == null) return;
 
        int n = _grid.size;
 
        if (_tex == null || _tex.width != n)
        {
            if (_tex != null) Destroy(_tex);
            _tex            = new Texture2D(n, n, TextureFormat.RGBA32, mipChain: false);
            _tex.filterMode = FilterMode.Point;
            _tex.wrapMode   = TextureWrapMode.Clamp;
            fertilityMap.texture = _tex;
        }
 
        var pixels = new Color[n * n];
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                pixels[x + (n - 1 - y) * n] = Color.Lerp(zeroColor, fullColor,
                                                           _grid.Get(x, y).fertility);
        _tex.SetPixels(pixels);
        _tex.Apply(updateMipmaps: false);
    }
}