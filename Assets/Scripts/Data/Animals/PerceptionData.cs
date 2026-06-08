using UnityEngine;

public struct PerceptionData
{
    public Vector2 toFood;
    public bool foodFound;
    public float foodDistance;

    public Vector2 toWater;
    public bool waterFound;

    public Vector2 socialVector;
    public int socialCount;

    /// <summary>
    /// Vettore di separazione: somma pesata delle direzioni
    /// che allontanano dai conspecifici troppo vicini.
    /// Indipendente da w_social — scala con separationForce nelle settings.
    /// </summary>
    public Vector2 separationVector;

    public Vector2 fleeVector;
    public bool predatorNearby;

    public Vector2 mateVector;
    public bool mateFound;
    public Animal mateCandidate;

    public Vector2 slopeVector;
    public float currentSlope;

    public Vector2 wanderVector;
}