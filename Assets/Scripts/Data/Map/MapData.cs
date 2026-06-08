// Assets/Scripts/Data/MapData.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Struttura dati principale del progetto.
/// Contiene tutto il necessario per ricostruire un mondo:
/// metadati, griglia, impostazioni di simulazione, e lista di entità da spawnare.
///
/// Serializzata in JSON da MapSaveManager.
/// Accessibile a runtime via MapSession.CurrentMap.
/// </summary>
[Serializable]
public class MapData
{
    public MapMetadata metadata;
    public WorldGrid grid;
    public SimulationSettings simulationSettings;
    public List<SpawnEntry> spawnEntries;

    public MapData()
    {
        metadata = new MapMetadata();
        simulationSettings = new SimulationSettings();
        spawnEntries = new List<SpawnEntry>();
    }

    /// <summary>
    /// Crea una MapData nuova con griglia vuota e settings di default.
    /// </summary>
    public static MapData CreateEmpty(string name, int gridSize = 32)
    {
        var data = new MapData
        {
            metadata = new MapMetadata
            {
                mapName = name,
                gridSize = gridSize,
            },
            grid = new WorldGrid(gridSize),
            simulationSettings = new SimulationSettings(),
            spawnEntries = new List<SpawnEntry>(),
        };
        return data;
    }
}