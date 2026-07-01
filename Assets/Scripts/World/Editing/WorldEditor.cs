using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldEditor : MonoBehaviour
{
    private WorldGrid grid => MapSession.Instance != null ? MapSession.Instance.CurrentMap?.grid : null;

    private WorldRenderer worldRenderer => WorldSession.Instance.Renderer;
    private Camera        editorCamera  => WorldSession.Instance.Camera.mainCamera; // Serve solo per i raycast

    private IEditorTool _activeTool;
    private bool _dragging;
    private bool _editorEnabled = true;

    // API pubblica

    public void SetTool(IEditorTool tool)
    {
        _activeTool?.OnDeactivate();
        _activeTool = tool;
        _activeTool?.OnActivate();
        _dragging = false;
    }

    public void SetEnabled(bool enabled)
    {
        _editorEnabled = enabled;
        if (!enabled) _activeTool?.OnDeactivate();
    }


    private void Update()
    {
        if (!_editorEnabled || _activeTool == null) return;

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        // BLOCCO UI: se il mouse è su un elemento UI, non processare input
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Se era in drag e il mouse va sulla UI termina il drag in modo pulito
            if (_dragging)
            {
                _dragging = false;
                // Ricalcola gradienti se il drag era su TerrainTool
                /*
                if (_activeTool is TerrainTool)
                    grid?.RecalculateGradients();
                */
            }
            return;
        }

        if (Mouse.current == null) return;

        bool leftDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool leftHeld = Mouse.current.leftButton.isPressed;
        bool leftReleased = Mouse.current.leftButton.wasReleasedThisFrame;

        // Le tre fasi di input con il click sinistro
        if (leftDown)
        {
            CellHit hit = Raycast();
            if (!hit.valid) return;
            _dragging = true;
            _activeTool.OnDragStart(hit);
        }

        if (_dragging && leftHeld)
        {
            CellHit hit = Raycast();
            if (hit.valid) _activeTool.OnDrag(hit);
        }

        if (_dragging && leftReleased)
        {
            CellHit hit = Raycast();
            _dragging = false;
            _activeTool.OnDragEnd(hit);
        }
    }

    private CellHit Raycast()
    {
        Ray ray = editorCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return new CellHit { valid = false };

        int x = Mathf.RoundToInt(hit.point.x / worldRenderer.config.cellSize);
        int y = Mathf.RoundToInt(hit.point.z / worldRenderer.config.cellSize);

        if (!grid.IsInside(x, y))
            return new CellHit { valid = false };

        return new CellHit(x, y, hit.point, grid.Get(x, y), hit.normal, true);
    }
}