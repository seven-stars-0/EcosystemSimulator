using UnityEngine;

[System.Serializable]
public class RenderConfig
{
    [Tooltip("Dimensione lato di una cella in unità Unity")]
    public float cellSize = 2f;

    [Tooltip("Scala verticale: height * heightScale = Y in Unity")]
    public float heightScale = 10f;
}