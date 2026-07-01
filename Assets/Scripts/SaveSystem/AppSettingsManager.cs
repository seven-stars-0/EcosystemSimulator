using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class AppSettingsManager : MonoBehaviour
{
    public static AppSettingsManager Instance { get; private set; }

    public AppSettings Settings { get; private set; }

    // Lista dei Material skybox nell'ordine corrispondente a AestheticsPanel.skyboxes[].sprite.
    [Header("Skyboxes")]
    [SerializeField] private Material[] skyboxMaterials;

    // Questa è SEMPRE salvata nel persistentDataPath, visto che non mi serviva nell'editor di Unity
    private string SavePath
        => Path.Combine(Application.persistentDataPath, "app_settings.json");


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    private void Start()
    {
        ApplyCameraSettings();
        // Lo skybox NON viene applicato qui, ma in WorldSession quando si apre l'editor o si avvia la simulazione (cedi prossimo metodo)
    }


    // Applica lo skybox corrispondente all'indice salvato.
    // Chiamato da WorldSession.EnterEditor() e WorldSession.EnterSimulation().
    public void ApplySkybox()
    {
        if (skyboxMaterials == null || skyboxMaterials.Length == 0) return;

        int index = Mathf.Clamp(Settings.skyboxIndex, 0, skyboxMaterials.Length - 1);
        var material = skyboxMaterials[index];

        if (material == null) return;

        UnityEngine.RenderSettings.skybox = material;
        DynamicGI.UpdateEnvironment();
    }

    // Applica le impostazioni camera a WorldCamera
    public void ApplyCameraSettings()
    {
        WorldSession.Instance?.Camera?.ApplySettings(Settings.camera);
    }

    public void Save()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AppSettingsManager] Save failed: {e.Message}");
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                Settings = new AppSettings();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AppSettingsManager] Load failed: {e.Message}");
            Settings = new AppSettings();
        }
    }

    private void OnApplicationQuit() => Save();
}
