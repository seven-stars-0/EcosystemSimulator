// Scripts/Data/CellData.cs
using Newtonsoft.Json;
using UnityEngine;

public enum ObstacleType { None, Rock, Tree }

[System.Serializable]
public class CellData
{
    // ── Dati persistenti (serializzati in JSON) ───────────────────────────────
    public float height;      // unità logiche. <0 = acqua implicita
    public float fertility;   // [0,1]
    public ObstacleType obstacle;    // ostacolo statico

    // ── Dati derivati (calcolati al salvataggio, letti a runtime) ─────────────
    public float gradientX;   // dh/dx
    public float gradientY;   // dh/dz
    public float slope;       // magnitude del gradiente

    // ── Proprietà calcolate ───────────────────────────────────────────────────
    [JsonIgnore] public bool IsWater => height < 0f;
    [JsonIgnore] public bool IsPassable => obstacle == ObstacleType.None && !IsWater;
    [JsonIgnore] public bool HasObstacle => obstacle != ObstacleType.None;

    // Vettore gradiente in coordinate griglia (xz), utile per gli agenti
    [JsonIgnore] public Vector2 GradientVec => new Vector2(gradientX, gradientY);

    public CellData()
    {
        height = 0f;
        fertility = 0.5f;
        obstacle = ObstacleType.None;
    }
}