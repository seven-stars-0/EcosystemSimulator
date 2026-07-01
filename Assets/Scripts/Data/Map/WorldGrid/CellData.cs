using Newtonsoft.Json;
using UnityEngine;

public enum ObstacleType { None, Rock, Tree }

[System.Serializable]
public class CellData
{
    // Dati persistenti
    public float height;
    public float fertility;          // [0,1]
    public ObstacleType obstacle;

    // Dati derivati (calcolati al salvataggio)
    // Il gradiente viene calcolato con le differenze finite centrali
    public float gradientX;   // dh/dx
    public float gradientY;   // dh/dz
    public float slope;       // magnitude del gradiente

    // Proprietà calcolate, non è necessario serializzarle
    [JsonIgnore] public bool IsWater => height < 0f;
    [JsonIgnore] public bool IsPassable => obstacle == ObstacleType.None && !IsWater;
    [JsonIgnore] public bool HasObstacle => obstacle != ObstacleType.None;

    // Vettore gradiente in coordinate griglia (xz), utile per gli agenti
    [JsonIgnore] public Vector2 GradientVec => new Vector2(gradientX, gradientY);

    public CellData()
    {
        height = 0f;
        fertility = 0.3f;
        obstacle = ObstacleType.None;
    }
}