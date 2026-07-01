using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class MapSaveManager : MonoBehaviour
{
    public static MapSaveManager Instance { get; private set; }

    // Versione dello schema dei parametri (SimulationSettings/MapData).
    // Questo è servito per gestire le differenze tra versione 0 (la precedente) e la 1 (definitiva)
    // Non serve più, ma lo lascio per ipotetiche versioni future
    public const int CurrentSchemaVersion = 1;

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

    // Lista mappe

    /// Metadati di tutte le mappe salvate. Deserializza MapData completa ma
    /// ritorna solo il campo metadata per evitare di tenere in RAM tutte le griglie.
    /// I file illeggibili vengono saltati con un warning.
    public List<MapMetadata> LoadAllMetadata()
    {
        var result = new List<MapMetadata>();

        string[] files;
        try { files = Directory.GetFiles(MapsFolder, "*.json"); }
        catch (Exception e)
        {
            Debug.LogWarning($"[MapSaveManager] Couldn't enlist the maps: {e.Message}");
            return result;
        }

        foreach (var file in files)
        {
            try
            {
                MapData data = JsonConvert.DeserializeObject<MapData>(File.ReadAllText(file));
                if (data?.metadata != null)
                {
                    data.metadata.filePath = file;
                    result.Add(data.metadata);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MapSaveManager] Error reading {file}: {e.Message}");
            }
        }

        return result;
    }

    // Carica la singola MapData completa che è stata selezionata
    public MapData Load(string filePath)
    {
        try
        {
            MapData data = JsonConvert.DeserializeObject<MapData>(File.ReadAllText(filePath));
            if (data == null) return null;

            if (data.metadata != null)
            {
                data.metadata.filePath = filePath;

                // In caso vengano caricate mappe con schemaVersion inferiore a quello attuale
                if (data.metadata.schemaVersion < CurrentSchemaVersion)
                    Debug.LogWarning(
                        $"[MapSaveManager] '{Path.GetFileName(filePath)}' has an old schema" +
                        $"(v{data.metadata.schemaVersion} < v{CurrentSchemaVersion}): simulation parameters " +
                        $"could be obsolete. Press the RESET button in ParameterPanel to adapt to current schema version.");
            }
            data.grid?.EnsureSlopesUpToDate();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapSaveManager] Load failed ({filePath}): {e.Message}");
            return null;
        }
    }

    // Salva una MapData (scrittura atomica). Ritorna true se riuscito
    public bool Save(MapData data)
    {
        if (data?.metadata == null)
        {
            Debug.LogWarning("[MapSaveManager] Save failed: data or metadata are null.");
            return false;
        }

        // Tecnicamente non serve più, ma lo lascio qui per eventuali future migliorie
        // data.grid?.RecalculateGradients();   // bake degli slope nel JSON (no-op se grid null)

        string newFilePath = Path.Combine(MapsFolder, MakeSafeFileName(data.metadata.mapName) + ".json");

        // Se la mappa era salvata con un nome diverso, rimuove il vecchio file.
        if (!string.IsNullOrEmpty(data.metadata.filePath)
            && data.metadata.filePath != newFilePath
            && File.Exists(data.metadata.filePath))
        {
            try { File.Delete(data.metadata.filePath); }
            catch (Exception e) { Debug.LogWarning($"[MapSaveManager] Failed to remove old-name map: {e.Message}"); }
        }

        data.metadata.filePath      = newFilePath;
        data.metadata.savedAt       = DateTime.Now.ToString("o");
        data.metadata.schemaVersion = CurrentSchemaVersion;

        try
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            // Scrittura ATOMICA: scrivi su file temporaneo, poi sostituisci.
            // Se il processo muore a metà, il file originale resta intatto.
            string tmp = newFilePath + ".tmp";
            File.WriteAllText(tmp, json);

            // IMPORTANTE: Questo IF ha come conseguenza il rimpiazzamento di eventuali mappe con lo stesso nome ma semanticamente diverse
            if (File.Exists(newFilePath)) File.Replace(tmp, newFilePath, null);
            else                          File.Move(tmp, newFilePath);

            data.metadata.isDirty = false;
            Debug.Log($"[MapSaveManager] Saved: {newFilePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapSaveManager] Save failed ({newFilePath}): {e.Message}");
            return false;
        }
    }

    /// Elimina una mappa dal disco
    public void Delete(MapMetadata meta)
    {
        if (meta == null || string.IsNullOrEmpty(meta.filePath)) return;
        try
        {
            if (File.Exists(meta.filePath)) File.Delete(meta.filePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MapSaveManager] Delete failed: {e.Message}");
        }
    }

    // Nome file sicuro dal nome mappa. Modifiche rispetto al nome visualizzato dall'utente:
    // 1. split sui caratteri non validi per il filesystem(join con "_")
    // 2. sostituzione degli spazi con "_"
    // 3. lowercase
    //
    // (3) ha come conseguenza che due mappe con le stesse lettere ma con diversi uppercase vengono mappate nello stesso nome
    // Fallback "map" se il risultato è vuoto.
    private static string MakeSafeFileName(string name)
    {
        string safe = string.Join("_", (name ?? "").Split(Path.GetInvalidFileNameChars()))
                            .Replace(" ", "_")
                            .ToLower();
        return string.IsNullOrEmpty(safe) ? "map" : safe;
    }
}
