using UnityEngine;

public struct CellHit
{
    // coordinate nella griglia
    public int x;
    public int y;

    // posizione precisa nel mondo
    public Vector3 worldPosition;

    // dati della cella
    public CellData cell;

    // eventuale normale del terreno
    public Vector3 normal;

    // validità hit
    public bool valid;

    public CellHit(
        int x,
        int y,
        Vector3 worldPosition,
        CellData cell,
        Vector3 normal,
        bool valid = true)
    {
        this.x = x;
        this.y = y;

        this.worldPosition = worldPosition;

        this.cell = cell;

        this.normal = normal;

        this.valid = valid;
    }
}