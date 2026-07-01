// Contratto per tutti i tool dell'editor di mappe.
// WorldEditor gestisce l'input e delega al tool attivo.
public interface IEditorTool
{
    // Chiamato una volta quando il tool diventa attivo
    void OnActivate();

    // Chiamato una volta quando il tool viene disattivato
    void OnDeactivate();

    // Click singolo (mouse giù e su senza drag)
    void OnClick(CellHit hit);

    // Inizio drag
    void OnDragStart(CellHit hit);

    // Drag in corso (chiamato ogni frame con il mouse giù)
    void OnDrag(CellHit hit);

    // Fine drag
    void OnDragEnd(CellHit hit);
}