using UnityEngine;
using UnityEngine.UI;

// ============================================================================
//  AestheticsPanel  -  Skybox + selettore circolare delle skin animali.
//  Lo skybox usa AppSettings.skyboxIndex; le skin delegano ad AnimalSkinManager
//  (che persiste i due indici in AppSettings). Tutto salvato tra le sessioni.
// ============================================================================

public class AestheticsPanel : MonoBehaviour
{
    // ── Skybox ─────────────────────────────────────────────────────────────────
    [Header("Skybox previews — stesso ordine di AppSettingsManager.skyboxMaterials")]
    [SerializeField] private Sprite[] skyboxPreviews;

    [Header("Skybox UI")]
    [SerializeField] private Image previewImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    // ── Skin animali ───────────────────────────────────────────────────────────
    [Header("Skin animali — Prede")]
    [SerializeField] private Image  preyPreview;
    [SerializeField] private Button preyPrevButton;
    [SerializeField] private Button preyNextButton;

    [Header("Skin animali — Predatori")]
    [SerializeField] private Image  predatorPreview;
    [SerializeField] private Button predatorPrevButton;
    [SerializeField] private Button predatorNextButton;

    private int _currentIndex;
    private AppSettings _settings;

    private void Awake()
    {
        prevButton.onClick.AddListener(NavigatePrev);
        nextButton.onClick.AddListener(NavigateNext);

        // Skin animali: la logica (scavalcamento + persistenza) e' in AnimalSkinManager.
        if (preyPrevButton     != null) preyPrevButton.onClick.AddListener(() => AdvanceSkin(prey: true,  dir: -1));
        if (preyNextButton     != null) preyNextButton.onClick.AddListener(() => AdvanceSkin(prey: true,  dir: +1));
        if (predatorPrevButton != null) predatorPrevButton.onClick.AddListener(() => AdvanceSkin(prey: false, dir: -1));
        if (predatorNextButton != null) predatorNextButton.onClick.AddListener(() => AdvanceSkin(prey: false, dir: +1));
    }

    private void OnEnable()
    {
        if (AnimalSkinManager.Instance != null)
            AnimalSkinManager.Instance.OnChanged += RefreshAnimalPreviews;
        RefreshAnimalPreviews();
    }

    private void OnDisable()
    {
        if (AnimalSkinManager.Instance != null)
            AnimalSkinManager.Instance.OnChanged -= RefreshAnimalPreviews;
    }

    public void Bind(AppSettings settings)
    {
        _settings = settings;
        _currentIndex = Mathf.Clamp(settings.skyboxIndex, 0, Mathf.Max(0, skyboxPreviews.Length - 1));

        AppSettingsManager.Instance.ApplySkybox();
        RefreshPreview();
        RefreshAnimalPreviews();
    }

    // ── Skybox: navigazione circolare ──────────────────────────────────────────

    private void NavigatePrev()
    {
        if (skyboxPreviews.Length == 0) return;
        _currentIndex = (_currentIndex - 1 + skyboxPreviews.Length) % skyboxPreviews.Length;
        ApplySkybox();
    }

    private void NavigateNext()
    {
        if (skyboxPreviews.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % skyboxPreviews.Length;
        ApplySkybox();
    }

    private void ApplySkybox()
    {
        if (_settings != null) _settings.skyboxIndex = _currentIndex;
        AppSettingsManager.Instance.ApplySkybox();
        RefreshPreview();
        AppSettingsManager.Instance.Save();
    }

    private void RefreshPreview()
    {
        if (previewImage == null || skyboxPreviews.Length == 0) return;
        int i = Mathf.Clamp(_currentIndex, 0, skyboxPreviews.Length - 1);
        previewImage.sprite = skyboxPreviews[i];
    }

    // ── Skin animali: navigazione (delegata) ──────────────────────────────────

    private void AdvanceSkin(bool prey, int dir)
    {
        var m = AnimalSkinManager.Instance;
        if (m == null) return;
        if (prey) m.AdvancePrey(dir); else m.AdvancePredator(dir);
        // l'anteprima si aggiorna via OnChanged (RefreshAnimalPreviews)
    }

    private void RefreshAnimalPreviews()
    {
        var m = AnimalSkinManager.Instance;
        if (m == null) return;
        if (preyPreview     != null) preyPreview.sprite     = m.PreySprite;
        if (predatorPreview != null) predatorPreview.sprite = m.PredatorSprite;
    }
}
