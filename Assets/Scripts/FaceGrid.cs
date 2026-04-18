using UnityEngine;
using System.Collections.Generic;

public class FaceGrid : MonoBehaviour
{
    public int faceIndex;
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float cellSize = 1f;

    private Dictionary<Vector2Int, ArrowTile> arrows = new Dictionary<Vector2Int, ArrowTile>();

    /// <summary>Registry toàn cục cho tra cứu xuyên mặt</summary>
    private static Dictionary<int, FaceGrid> allFaces = new Dictionary<int, FaceGrid>();

    private void OnEnable() { allFaces[faceIndex] = this; }
    private void OnDisable()
    {
        if (allFaces.ContainsKey(faceIndex) && allFaces[faceIndex] == this)
            allFaces.Remove(faceIndex);
    }

    public static FaceGrid GetFace(int index)
    {
        allFaces.TryGetValue(index, out FaceGrid grid);
        return grid;
    }

    public static void ClearAllFaces() { allFaces.Clear(); }

    // --- Đăng ký MỘT ô ---
    public void RegisterArrow(ArrowTile arrow)
    {
        if (!arrows.ContainsKey(arrow.GridCoords)) arrows[arrow.GridCoords] = arrow;
    }

    public void UnregisterArrow(ArrowTile arrow)
    {
        if (arrows.ContainsKey(arrow.GridCoords) && arrows[arrow.GridCoords] == arrow)
            arrows.Remove(arrow.GridCoords);
    }

    // --- Đăng ký NHIỀU ô (body cells) ---
    public void RegisterArrowCells(ArrowTile arrow, List<Vector2Int> cells)
    {
        foreach (var cell in cells)
        {
            if (!IsOutOfBounds(cell))
                arrows[cell] = arrow;
        }
    }

    public void UnregisterArrowCells(ArrowTile arrow, List<Vector2Int> cells)
    {
        foreach (var cell in cells)
        {
            if (arrows.ContainsKey(cell) && arrows[cell] == arrow)
                arrows.Remove(cell);
        }
    }

    public ArrowTile GetArrowAt(Vector2Int coords)
    {
        if (arrows.TryGetValue(coords, out ArrowTile arr)) return arr;
        return null;
    }

    public Vector3 GetLocalPosition(Vector2Int coords)
    {
        float startX = -(gridWidth - 1) * cellSize / 2f;
        float startY = -(gridHeight - 1) * cellSize / 2f;
        return new Vector3(startX + coords.x * cellSize, startY + coords.y * cellSize, 0f);
    }

    public Vector3 GetWorldPosition(Vector2Int coords)
    {
        return transform.TransformPoint(GetLocalPosition(coords));
    }

    public Vector3 GetEdgeWorldPosition(Vector2Int lastGridPos, Vector2Int dir)
    {
        Vector3 localPos = GetLocalPosition(lastGridPos);
        localPos += new Vector3(dir.x, dir.y, 0f) * (cellSize / 2f);
        return transform.TransformPoint(localPos);
    }

    public bool IsOutOfBounds(Vector2Int coords)
    {
        return coords.x < 0 || coords.x >= gridWidth || coords.y < 0 || coords.y >= gridHeight;
    }
}
