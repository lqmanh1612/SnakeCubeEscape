using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private Vector2 lastMousePos;
    private bool isDragging = false;
    private float dragThreshold = 5f; // Pixels to consider as drag
    private Vector2 startClickPos;

    void Update()
    {
        if (Pointer.current == null) return;

        // Touch or Mouse Logic
        if (Pointer.current.press.wasPressedThisFrame)
        {
            startClickPos = Pointer.current.position.ReadValue();
            lastMousePos = Pointer.current.position.ReadValue();
            isDragging = false;
        }

        if (Pointer.current.press.isPressed)
        {
            Vector2 mousePos = Pointer.current.position.ReadValue();
            Vector2 delta = mousePos - lastMousePos;

            // Check if dragged far enough
            if (Vector2.Distance(startClickPos, mousePos) > dragThreshold)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                // Send delta to pivot
                if (StructurePivot.Instance != null && delta != Vector2.zero)
                {
                    StructurePivot.Instance.RotatePivot(delta);
                }
            }

            lastMousePos = mousePos;
        }

        if (Pointer.current.press.wasReleasedThisFrame)
        {
            if (!isDragging) // It was a tap
            {
                HandleTap(Pointer.current.position.ReadValue());
            }
            isDragging = false;
        }
    }

    private void HandleTap(Vector2 screenPos)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if we tapped an ArrowTile
            ArrowTile arrow = hit.collider.GetComponent<ArrowTile>();
            if (arrow != null)
            {
                arrow.Slide();
            }
        }
    }
}
