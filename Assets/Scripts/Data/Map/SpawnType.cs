using System;

/// Tipo serializzato in MapData.spawnEntries.
/// Non include ostacoli: quelli stanno in CellData.obstacle.
public enum SpawnType { Prey, Predator, Plant }

// Tutto ciò che SpawnTool può piazzare, inclusi gli ostacoli statici.
public enum SpawnableType { Prey, Predator, Plant, Tree, Rock }

// Entità piazzata dall'utente con SpawnTool.
// worldX/worldZ in unità mondo; Y viene interpolata da WorldGrid.SampleHeight().
[Serializable]
public class SpawnEntry
{
    public SpawnType type;
    public float worldX;
    public float worldZ;
}
