using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 1 ô thuộc thân mũi tên — có thể nằm trên bất kỳ mặt nào.
/// </summary>
[System.Serializable]
public class BodyCell
{
    public int faceIndex;
    public Vector2Int position;
}

[System.Serializable]
public class ArrowSpawnData
{
    [Tooltip("Vị trí đầu mũi tên (Arrowhead) trên lưới")]
    public Vector2Int position;
    
    [Tooltip("Hướng bay khi bấm: (0,1)=Lên, (1,0)=Phải, (0,-1)=Xuống, (-1,0)=Trái")]
    public Vector2Int initialDirection;
    
    [Header("Thân Mũi Tên (vẽ tay)")]
    [Tooltip("Danh sách ô thân theo thứ tự nối tiếp từ đầu mũi tên ra phía sau")]
    public List<BodyCell> bodyCells = new List<BodyCell>();

    /// <summary>
    /// Lấy TẤT CẢ ô (bao gồm đầu mũi tên) theo thứ tự.
    /// </summary>
    public List<BodyCell> GetAllCells(int headFaceIndex)
    {
        var all = new List<BodyCell>();
        all.Add(new BodyCell { faceIndex = headFaceIndex, position = position });
        if (bodyCells != null) all.AddRange(bodyCells);
        return all;
    }

    /// <summary>
    /// Lấy chỉ các ô trên 1 mặt cụ thể (bao gồm đầu nếu cùng mặt).
    /// </summary>
    public List<Vector2Int> GetCellsOnFace(int faceIndex, int headFaceIndex)
    {
        var cells = new List<Vector2Int>();
        if (headFaceIndex == faceIndex) cells.Add(position);
        if (bodyCells != null)
        {
            foreach (var c in bodyCells)
                if (c.faceIndex == faceIndex) cells.Add(c.position);
        }
        return cells;
    }
}

[System.Serializable]
public class PathStep
{
    public int faceIndex;
    public Vector2Int gridPos;
    public Vector3 worldPos;
    public bool isCrossEdge;
}

[System.Serializable]
public class FaceData
{
    public string faceName;
    public List<ArrowSpawnData> arrows = new List<ArrowSpawnData>();
}

[CreateAssetMenu(fileName = "NewArrowLevel", menuName = "Arrow Escape/Level Data")]
public class ArrowLevelData : ScriptableObject
{
    [Range(3, 10)]
    public int gridSize = 5;
    
    public FaceData[] faces = new FaceData[6];

    private void OnEnable()
    {
        if (faces == null || faces.Length != 6) return;
        if (string.IsNullOrEmpty(faces[0].faceName))
        {
            faces[0].faceName = "0_Front";
            faces[1].faceName = "1_Back";
            faces[2].faceName = "2_Left";
            faces[3].faceName = "3_Right";
            faces[4].faceName = "4_Top";
            faces[5].faceName = "5_Bottom";
        }
    }
}
