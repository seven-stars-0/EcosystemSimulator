/// <summary>
/// Stato di una pianta per singola cella della griglia.
/// Gestito interamente da PlantManager.
/// Non serializzato: viene ricostruito all'avvio della simulazione.
/// </summary>
public class PlantState
{
    /// <summary>La pianta esiste in questa cella.</summary>
    public bool hasPlant;

    /// <summary>La pianta ha frutti maturi e può essere mangiata dalle prede.</summary>
    public bool hasFruit;

    /// <summary>
    /// Timer per la crescita del frutto [s].
    /// Decrementato da PlantManager ogni tick.
    /// Quando raggiunge 0: hasFruit = true.
    /// </summary>
    public float fruitTimer;

    /// <summary>
    /// Timer per la morte naturale [s].
    /// Quando raggiunge 0: la pianta muore (hasPlant = false).
    /// </summary>
    public float deathTimer;

    // ── Tipo ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// True = pianta piazzata dall'editor (non muore mai spontaneamente,
    /// ma i suoi frutti si consumano normalmente).
    /// </summary>
    public bool isPermanent;

    // ── Coordinata griglia (per lookup rapido dal PlantManager) ──────────────
    public int gridX;
    public int gridY;
}