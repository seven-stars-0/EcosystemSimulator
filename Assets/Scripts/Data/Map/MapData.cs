using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// Struttura dati principale del progetto.
/// Contiene tutto il necessario per ricostruire un mondo:
/// metadati, griglia, impostazioni di simulazione, e lista di entità da spawnare.
///
/// Serializzata in JSON da MapSaveManager.
/// Accessibile a runtime via MapSession.CurrentMap.
[Serializable]
public class MapData
{
    public MapMetadata metadata;
    public WorldGrid grid;
    public SimulationSettings simulationSettings;
    public List<SpawnEntry> spawnEntries;

    // Spawn casuale all'avvio: oltre agli SpawnEntry piazzati a mano, vengono
    // generati questi N prede e M predatori in celle adatte (height>0 e senza ostacoli)
    public int randomPreyCount;
    public int randomPredatorCount;

    public MapData()
    {
        metadata = new MapMetadata();
        simulationSettings = new SimulationSettings();
        spawnEntries = new List<SpawnEntry>();
    }

    // Crea una MapData nuova con griglia vuota e settings di default.
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
