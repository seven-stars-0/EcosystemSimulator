/// <summary>
/// Interfaccia per tutti i layer visuali gestiti da WorldRenderer.
/// Ogni View sa costruirsi a partire da WorldGrid + RenderConfig.
/// </summary>
public interface IWorldView
{
    /// <summary>
    /// Costruisce (o ricostruisce completamente) la visualizzazione.
    /// Chiamato alla prima inizializzazione e dopo resize della griglia.
    /// </summary>
    void Build(WorldGrid grid, RenderConfig config);

    /// <summary>
    /// Aggiornamento leggero: aggiorna solo i dati cambiati.
    /// Se non implementato diversamente da Build, può fare lo stesso.
    /// </summary>
    void Refresh(WorldGrid grid, RenderConfig config);

    /// <summary>Mostra/nasconde il layer senza distruggerlo.</summary>
    void SetVisible(bool visible);
}