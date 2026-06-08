using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class MapSaveManager : MonoBehaviour
{
    public static MapSaveManager Instance { get; private set; }

    private string MapsFolder
    {
        get
        {
#if UNITY_EDITOR
            string path = Path.Combine(Application.dataPath, "Maps");
#else
            string path = Path.Combine(Application.persistentDataPath, "Maps");
#endif
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Lista mappe ───────────────────────────────────────────────────────────

    /// <summary>
    /// Restituisce i metadati di tutte le mappe salvate.
    /// Deserializza MapData completa ma ritorna solo il campo metadata —
    /// evita di caricare la griglia intera solo per mostrare la lista.
    /// </summary>
    public List<MapMetadata> LoadAllMetadata()
    {
        var result = new List<MapMetadata>();

        foreach (var file in Directory.GetFiles(MapsFolder, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                MapData data = JsonConvert.DeserializeObject<MapData>(json);

                if (data?.metadata != null)
                {
                    data.metadata.filePath = file;
                    result.Add(data.metadata);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSaveManager] Errore lettura {file}: {e.Message}");
            }
        }

        return result;
    }

    /// <summary>Carica la MapData completa da file.</summary>
    public MapData Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        MapData data = JsonConvert.DeserializeObject<MapData>(json);
        if (data != null)
        {
            data.metadata.filePath = filePath;
            data.grid?.EnsureSlopesUpToDate();
        }
        return data;
    }

    /// <summary>Salva una MapData.</summary>
    // In MapSaveManager.Save() — sostituisci il metodo

    public void Save(MapData data)
    {
        data.grid.RecalculateGradients(); // bake slopes once into JSON

        string safeFileName = MakeSafeFileName(data.metadata.mapName);
        string newFilePath = Path.Combine(MapsFolder, safeFileName + ".json");

        // Se esiste un file precedente con nome diverso, cancellalo
        if (!string.IsNullOrEmpty(data.metadata.filePath)
            && data.metadata.filePath != newFilePath
            && File.Exists(data.metadata.filePath))
        {
            File.Delete(data.metadata.filePath);
            Debug.Log($"[MapSaveManager] Vecchio file rimosso: {data.metadata.filePath}");
        }

        data.metadata.filePath = newFilePath;
        data.metadata.savedAt = DateTime.Now.ToString("o");

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(newFilePath, json);

        data.metadata.isDirty = false;
        Debug.Log($"[MapSaveManager] Salvato: {newFilePath}");
    }

    /// <summary>Elimina una mappa dal disco.</summary>
    public void Delete(MapMetadata meta)
    {
        if (File.Exists(meta.filePath))
            File.Delete(meta.filePath);
    }

    private static string MakeSafeFileName(string name)
        => string.Join("_", name.Split(Path.GetInvalidFileNameChars()))
                 .Replace(" ", "_")
                 .ToLower();
}