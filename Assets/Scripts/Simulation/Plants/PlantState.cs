// Stato di una pianta per singola cella della griglia.
// Gestito interamente da PlantManager.
public class PlantState
{
    // La pianta esiste in questa cella
    public bool hasPlant;

    // La pianta ha frutti maturi che possono essere mangiati dalle prede
    public bool hasFruit;

    // Timer per la crescita del frutto [s].
    // Decrementato da PlantManager ogni tick.
    // Quando raggiunge 0 poniamo hasFruit = true.
    public float fruitTimer;

    // True se è una pianta piazzata dall'utente
    // Non muore mai spontaneamente, ma i suoi frutti si consumano normalmente
    public bool isPermanent;

    public int gridX;
    public int gridY;
}
