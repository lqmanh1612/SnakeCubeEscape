using UnityEngine;

/// <summary>
/// Bảng tra cứu kết nối giữa 6 mặt của khối lập phương.
/// Khi 1 mũi tên ở Face A đi ra khỏi mép, CubeTopology cho biết:
///   - Nó sẽ nhảy sang Face nào (neighborFace)
///   - Toạ độ 2D trên Face mới là bao nhiêu (TransformCoord)
///   - Hướng di chuyển 2D mới trên Face đó (TransformDir)
///
/// Quy ước Face Index:
///   0 = Front  (nhìn về phía -Z)
///   1 = Back   (nhìn về phía +Z)
///   2 = Left   (nhìn về phía -X)
///   3 = Right  (nhìn về phía +X)  
///   4 = Top    (nhìn về phía +Y)
///   5 = Bottom (nhìn về phía -Y)
///
/// Quy ước hướng 2D trên mỗi mặt:
///   (0,1)  = lên     (1,0)  = phải
///   (0,-1) = xuống   (-1,0) = trái
/// </summary>
public static class CubeTopology
{
    public struct EdgeTransition
    {
        public int neighborFace;          // Face index đích
        public bool flipAxis;             // Có đảo trục ngang/dọc hay không
        public bool reverseCoord;         // Có đảo ngược toạ độ trên trục giữ nguyên không
        public EdgeSide entryEdge;        // Mũi tên nhập vào face mới từ cạnh nào
    }

    public enum EdgeSide { Bottom, Top, Left, Right }

    /// <summary>
    /// Trả về mặt lân cận và thông tin biến đổi khi mũi tên rời Face `faceIndex`
    /// theo cạnh `exitEdge`.
    /// </summary>
    public static EdgeTransition GetNeighbor(int faceIndex, EdgeSide exitEdge)
    {
        // Bảng kết nối 6 mặt x 4 cạnh = 24 trường hợp
        // Mỗi entry: (neighborFace, entryEdge vào mặt mới)
        switch (faceIndex)
        {
            case 0: // Front
                switch (exitEdge)
                {
                    case EdgeSide.Top:    return new EdgeTransition { neighborFace = 4, entryEdge = EdgeSide.Bottom };
                    case EdgeSide.Bottom: return new EdgeTransition { neighborFace = 5, entryEdge = EdgeSide.Top };
                    case EdgeSide.Left:   return new EdgeTransition { neighborFace = 2, entryEdge = EdgeSide.Right };
                    case EdgeSide.Right:  return new EdgeTransition { neighborFace = 3, entryEdge = EdgeSide.Left };
                }
                break;
            case 1: // Back
                switch (exitEdge)
                {
                    case EdgeSide.Top:    return new EdgeTransition { neighborFace = 4, entryEdge = EdgeSide.Top, reverseCoord = true };
                    case EdgeSide.Bottom: return new EdgeTransition { neighborFace = 5, entryEdge = EdgeSide.Bottom, reverseCoord = true };
                    case EdgeSide.Left:   return new EdgeTransition { neighborFace = 3, entryEdge = EdgeSide.Right };
                    case EdgeSide.Right:  return new EdgeTransition { neighborFace = 2, entryEdge = EdgeSide.Left };
                }
                break;
            case 2: // Left
                switch (exitEdge)
                {
                    case EdgeSide.Top:    return new EdgeTransition { neighborFace = 4, entryEdge = EdgeSide.Left };
                    case EdgeSide.Bottom: return new EdgeTransition { neighborFace = 5, entryEdge = EdgeSide.Left, reverseCoord = true };
                    case EdgeSide.Left:   return new EdgeTransition { neighborFace = 1, entryEdge = EdgeSide.Right };
                    case EdgeSide.Right:  return new EdgeTransition { neighborFace = 0, entryEdge = EdgeSide.Left };
                }
                break;
            case 3: // Right
                switch (exitEdge)
                {
                    case EdgeSide.Top:    return new EdgeTransition { neighborFace = 4, entryEdge = EdgeSide.Right };
                    case EdgeSide.Bottom: return new EdgeTransition { neighborFace = 5, entryEdge = EdgeSide.Right, reverseCoord = true };
                    case EdgeSide.Left:   return new EdgeTransition { neighborFace = 0, entryEdge = EdgeSide.Right };
                    case EdgeSide.Right:  return new EdgeTransition { neighborFace = 1, entryEdge = EdgeSide.Left };
                }
                break;
            case 4: // Top
                switch (exitEdge)
                {
                    case EdgeSide.Top:    return new EdgeTransition { neighborFace = 1, entryEdge = EdgeSide.Top, reverseCoord = true };
                    case EdgeSide.Bottom: return new EdgeTransition { neighborFace = 0, entryEdge = EdgeSide.Top };
                    case EdgeSide.Left:   return new EdgeTransition { neighborFace = 2, entryEdge = EdgeSide.Top };
                    case EdgeSide.Right:  return new EdgeTransition { neighborFace = 3, entryEdge = EdgeSide.Top };
                }
                break;
            case 5: // Bottom
                switch (exitEdge)
                {
                    case EdgeSide.Top:    return new EdgeTransition { neighborFace = 0, entryEdge = EdgeSide.Bottom };
                    case EdgeSide.Bottom: return new EdgeTransition { neighborFace = 1, entryEdge = EdgeSide.Bottom, reverseCoord = true };
                    case EdgeSide.Left:   return new EdgeTransition { neighborFace = 2, entryEdge = EdgeSide.Bottom, reverseCoord = true };
                    case EdgeSide.Right:  return new EdgeTransition { neighborFace = 3, entryEdge = EdgeSide.Bottom, reverseCoord = true };
                }
                break;
        }
        // Fallback (should not happen)
        return new EdgeTransition { neighborFace = faceIndex, entryEdge = exitEdge };
    }

    /// <summary>
    /// Từ hướng di chuyển 2D trên lưới, xác định cạnh thoát ra.
    /// </summary>
    public static EdgeSide DirToEdge(Vector2Int dir)
    {
        if (dir.y > 0) return EdgeSide.Top;
        if (dir.y < 0) return EdgeSide.Bottom;
        if (dir.x < 0) return EdgeSide.Left;
        return EdgeSide.Right;
    }

    /// <summary>
    /// Tính toạ độ + hướng mới khi mũi tên nhảy qua cạnh sang face khác.
    /// exitPos: toạ độ cuối cùng trên face cũ (sát mép)
    /// exitDir: hướng đang đi trên face cũ
    /// gridSize: kích thước lưới (VD: 5)
    /// Trả về: (newFace, newPos, newDir)
    /// </summary>
    public static (int newFace, Vector2Int newPos, Vector2Int newDir) CrossEdge(
        int currentFace, Vector2Int exitPos, Vector2Int exitDir, int gridSize)
    {
        EdgeSide exitEdge = DirToEdge(exitDir);
        EdgeTransition t = GetNeighbor(currentFace, exitEdge);
        int maxIdx = gridSize - 1;

        // Toạ độ "song song với mép" — giá trị được giữ lại khi nhảy face
        int parallelCoord;
        if (exitDir.x != 0) // Di chuyển ngang → toạ độ song song mép = y
            parallelCoord = exitPos.y;
        else // Di chuyển dọc → toạ độ song song mép = x
            parallelCoord = exitPos.x;

        if (t.reverseCoord)
            parallelCoord = maxIdx - parallelCoord;

        // Xác định toạ độ + hướng mới trên face đích dựa vào cạnh nhập
        Vector2Int newPos;
        Vector2Int newDir;

        switch (t.entryEdge)
        {
            case EdgeSide.Bottom: // Nhập từ đáy → y=0, đi lên
                newPos = new Vector2Int(parallelCoord, 0);
                newDir = new Vector2Int(0, 1);
                break;
            case EdgeSide.Top: // Nhập từ đỉnh → y=max, đi xuống
                newPos = new Vector2Int(parallelCoord, maxIdx);
                newDir = new Vector2Int(0, -1);
                break;
            case EdgeSide.Left: // Nhập từ trái → x=0, đi phải
                newPos = new Vector2Int(0, parallelCoord);
                newDir = new Vector2Int(1, 0);
                break;
            case EdgeSide.Right: // Nhập từ phải → x=max, đi trái
                newPos = new Vector2Int(maxIdx, parallelCoord);
                newDir = new Vector2Int(-1, 0);
                break;
            default:
                newPos = exitPos;
                newDir = exitDir;
                break;
        }

        return (t.neighborFace, newPos, newDir);
    }
}
