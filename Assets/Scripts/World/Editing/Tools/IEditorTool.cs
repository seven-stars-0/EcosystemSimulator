/// <summary>
/// Contratto per tutti i tool dell'editor di mappe.
/// WorldEditor gestisce l'input e delega al tool attivo.
/// </summary>
public interface IEditorTool
{
    string ToolName { get; }   // per la UI

    /// <summary>Chiamato una volta quando il tool diventa attivo.</summary>
    void OnActivate();

    /// <summary>Chiamato una volta quando il tool viene disattivato.</summary>
    void OnDeactivate();

    /// <summary>Click singolo (mouse giù e su senza drag).</summary>
    void OnClick(CellHit hit);

    /// <summary>Inizio drag.</summary>
    void OnDragStart(CellHit hit);

    /// <summary>Drag in corso (chiamato ogni frame con il mouse giù).</summary>
    void OnDrag(CellHit hit);

    /// <summary>Fine drag.</summary>
    void OnDragEnd(CellHit hit);
}