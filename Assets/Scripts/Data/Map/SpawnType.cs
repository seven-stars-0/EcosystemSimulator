// Assets/Scripts/Data/MapData/SpawnType.cs
using System;

/// <summary>
/// Tipo serializzato in MapData.spawnEntries.
/// Non include ostacoli: quelli stanno in CellData.obstacle.
/// </summary>
public enum SpawnType { Prey, Predator, Plant }

/// <summary>
/// Tutto ciò che SpawnTool può piazzare, inclusi gli ostacoli statici.
/// </summary>
public enum SpawnableType { Prey, Predator, Plant, Tree, Rock }

/// <summary>
/// Entità "viva" serializzata nella mappa.
/// worldX/worldZ in unità mondo; Y viene interpolata da WorldGrid.SampleHeight().
/// </summary>
[Serializable]
public class SpawnEntry
{
    public SpawnType type;
    public float worldX;
    public float worldZ;
}