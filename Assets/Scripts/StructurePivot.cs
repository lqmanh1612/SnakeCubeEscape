using UnityEngine;
using DG.Tweening;

public class StructurePivot : MonoBehaviour
{
    public static StructurePivot Instance { get; private set; }

    [Header("Rotation Settings")]
    public float rotationSpeed = 0.5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RotatePivot(Vector2 dragDelta)
    {
        // Delta comes from horizontal/vertical swipe
        // Horizontal swipe rotates around World Y axis
        // Vertical swipe rotates around Camera Right axis (or Local X depending on design)
        // For hyper-casual puzzle, usually rotating around Space.World Y and X makes sense.

        float rotX = dragDelta.y * rotationSpeed; // Up/down swipe rotates around X axis
        float rotY = -dragDelta.x * rotationSpeed; // Left/right swipe rotates around Y axis

        // Apply rotation
        transform.Rotate(Vector3.up, rotY, Space.World);
        transform.Rotate(Vector3.right, rotX, Space.World);
    }
}
