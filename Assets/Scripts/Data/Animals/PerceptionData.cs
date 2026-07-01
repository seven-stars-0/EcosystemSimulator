using UnityEngine;

public struct PerceptionData
{
    public Vector2 toFood;        // dir unitaria al cibo (pianta per prede / preda per predatori)
    public bool    foodFound;

    public Vector2 fleeDir;       // dir unitaria di fuga dai predatori (prede)
    public bool    predatorNearby;

    public Vector2 cohesionDir;   // dir unitaria verso il centroide dei conspecifici
    public Vector2 alignmentDir;  // dir unitaria = heading medio dei conspecifici (boids alignment)
    public Vector2 separation;    // spinta di allontanamento (somma pesata, puo' superare 1 se affollato)
    public Vector2 wanderDir;     // dir unitaria di esplorazione (random walk smussato)

    public int neighborCount;
}
