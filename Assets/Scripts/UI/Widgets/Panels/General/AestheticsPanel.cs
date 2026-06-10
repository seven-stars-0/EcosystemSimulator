using System;
using UnityEngine;
using UnityEngine.UI;

public class AestheticsPanel : MonoBehaviour
{
    // Solo gli sprite di anteprima — i Material sono in AppSettingsManager.
    // Devono essere nello stesso ordine di AppSettingsManager.skyboxMaterials.
    [Header("Skybox previews — stesso ordine di AppSettingsManager.skyboxMaterials")]
    [SerializeField] private Sprite[] skyboxPreviews;

    [Header("UI")]
    [SerializeField] private Image previewImage;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int _currentIndex;
    private AppSettings _settings;

    private void Awake()
    {
        prevButton.onClick.AddListener(NavigatePrev);
        nextButton.onClick.AddListener(NavigateNext);
    }

    public void Bind(AppSettings settings)
    {
        _settings = settings;
        _currentIndex = Mathf.Clamp(
            settings.skyboxIndex, 0,
            Mathf.Max(0, skyboxPreviews.Length - 1));

        // Applica e mostra anteprima dello skybox attuale
        AppSettingsManager.Instance.ApplySkybox();
        RefreshPreview();
    }

    // ── Navigazione circolare ─────────────────────────────────────────────────

    private void NavigatePrev()
    {
        if (skyboxPreviews.Length == 0) return;
        _currentIndex = (_currentIndex - 1 + skyboxPreviews.Length) % skyboxPreviews.Length;
        Apply();
    }

    private void NavigateNext()
    {
        if (skyboxPreviews.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % skyboxPreviews.Length;
        Apply();
    }

    private void Apply()
    {
        // Aggiorna l'indice nelle settings e applica il nuovo skybox live
        if (_settings != null)
            _settings.skyboxIndex = _currentIndex;

        AppSettingsManager.Instance.ApplySkybox();
        RefreshPreview();

        // Salva subito su disco
        AppSettingsManager.Instance.Save();
    }

    private void RefreshPreview()
    {
        if (previewImage == null || skyboxPreviews.Length == 0) return;
        int i = Mathf.Clamp(_currentIndex, 0, skyboxPreviews.Length - 1);
        previewImage.sprite = skyboxPreviews[i];
    }
}