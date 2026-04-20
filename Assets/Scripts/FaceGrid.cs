using UnityEngine;
using System.Collections.Generic;

public class FaceGrid : MonoBehaviour
{
    public int faceIndex = -1;
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

    /// <summary>
    /// Gán faceIndex và đăng ký lại vào từ điển tĩnh. 
    /// FIX lỗi khi AddComponent ở runtime, OnEnable chạy trước khi faceIndex được gán.
    /// </summary>
    public void SetFaceIndex(int index)
    {
        if (allFaces.ContainsKey(faceIndex) && allFaces[faceIndex] == this)
        {
            allFaces.Remove(faceIndex);
        }
        faceIndex = index;
        allFaces[faceIndex] = this;
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
        
        float localX = startX + coords.x * cellSize;
        // Các mặt xung quanh (Front, Back, Left, Right) hướng ra ngoài, nên trục X local bị lật ngược (nhìn từ ngoài vào).
        // Ta lật ngược lại localX để 'Data x=0' luôn luôn là "Cạnh Trái" (Visual Left).
        if (faceIndex < 4) localX = -localX;

        float localY = startY + coords.y * cellSize;
        // Mặt Bottom nằm ở dưới Front trong editor, nhưng bị rotate làm lật ngược y.
        // Ta lật y lại để đồng bộ visual kéo xuống.
        if (faceIndex == 5) localY = -localY;

        return new Vector3(localX, localY, 0f);
    }

    public Vector3 GetWorldPosition(Vector2Int coords)
    {
        return transform.TransformPoint(GetLocalPosition(coords));
    }

    public Vector3 GetEdgeWorldPosition(Vector2Int lastGridPos, Vector2Int dir)
    {
        Vector3 localPos = GetLocalPosition(lastGridPos);
        
        // Hướng đi cũng cần được lật ngược X để khớp với Data
        float abstractDirX = dir.x * (cellSize / 2f);
        if (faceIndex < 4) abstractDirX = -abstractDirX;

        float abstractDirY = dir.y * (cellSize / 2f);
        if (faceIndex == 5) abstractDirY = -abstractDirY;

        localPos += new Vector3(abstractDirX, abstractDirY, 0f);
        return transform.TransformPoint(localPos);
    }

    public bool IsOutOfBounds(Vector2Int coords)
    {
        return coords.x < 0 || coords.x >= gridWidth || coords.y < 0 || coords.y >= gridHeight;
    }
}
