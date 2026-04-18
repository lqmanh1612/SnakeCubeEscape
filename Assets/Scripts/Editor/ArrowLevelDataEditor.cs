using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(ArrowLevelData))]
public class ArrowLevelDataEditor : Editor
{
    private int selectedFaceIndex = 0;

    // ======== DRAW MODE STATE (static vì Editor có thể bị tạo lại) ========
    private static bool isDrawing = false;
    private static int drawFace = -1;   // Face chứa arrowhead đang vẽ
    private static int drawIndex = -1;  // Index trong face.arrows

    private Vector2Int[] dirs = {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0)   // Left
    };
    private string[] arrows = { "↑", "→", "↓", "←" };

    public override void OnInspectorGUI()
    {
        ArrowLevelData data = (ArrowLevelData)target;
        EditorGUI.BeginChangeCheck();

        // Validate draw state
        ValidateDrawState(data);

        // ======== BƯỚC 1: GRID SIZE ========
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("BƯỚC 1: Cấu Hình Ma Trận", EditorStyles.boldLabel);
        data.gridSize = EditorGUILayout.IntSlider("Size (VD: 3x3 hoặc 5x5)", data.gridSize, 3, 10);

        EditorGUILayout.Space();

        // ======== BƯỚC 2: CHỌN MẶT (với highlight cross-face) ========
        EditorGUILayout.LabelField("BƯỚC 2: Chọn Mặt Của Khối", EditorStyles.boldLabel);

        // Tính valid next cells để biết face nào có thể vẽ tiếp
        var validNexts = isDrawing ? GetValidNextCells(data) : null;

        GUILayout.BeginHorizontal();
        for (int i = 0; i < 6; i++)
        {
            bool isSelected = (selectedFaceIndex == i);
            bool canDrawHere = validNexts != null && validNexts.Exists(v => v.face == i);

            if (isSelected)
                GUI.backgroundColor = Color.green;
            else if (isDrawing && canDrawHere)
                GUI.backgroundColor = Color.yellow; // Gợi ý: có thể vẽ sang mặt này!
            else
                GUI.backgroundColor = Color.white;

            if (GUILayout.Button(data.faces[i].faceName, EditorStyles.miniButton, GUILayout.Height(25)))
                selectedFaceIndex = i;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // ======== BƯỚC 3: LƯỚI TƯƠNG TÁC ========
        FaceData currentFace = data.faces[selectedFaceIndex];
        if (currentFace.arrows == null) currentFace.arrows = new List<ArrowSpawnData>();

        // Draw mode banner
        if (isDrawing)
        {
            EditorGUILayout.BeginHorizontal("helpbox");
            GUI.backgroundColor = new Color(0.4f, 0.9f, 1f);
            EditorGUILayout.LabelField("✏️ CHẾ ĐỘ VẼ THÂN — Click ô vàng để mở rộng, click ô đỏ để xoá cuối", EditorStyles.boldLabel);
            if (GUILayout.Button("❌ Xong", GUILayout.Width(60), GUILayout.Height(20)))
            {
                isDrawing = false;
                drawFace = -1;
                drawIndex = -1;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField($"BƯỚC 3: Vẽ Mũi Tên ({data.faces[selectedFaceIndex].faceName})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click ô trống → đặt mũi tên ↑. Click tiếp → xoay → xoá.\nSau đó ấn ✏️ ở Bước 4 để vẽ thân.", MessageType.None);
        }

        // Pre-compute hiển thị
        HashSet<string> bodyCellSet = new HashSet<string>();
        HashSet<string> headCellSet = new HashSet<string>();
        Dictionary<string, int> cellToArrowIndex = new Dictionary<string, int>();

        // Duyệt TẤT CẢ arrow trên TẤT CẢ face để tìm body cells hiển thị trên face hiện tại
        for (int f = 0; f < 6; f++)
        {
            if (data.faces[f].arrows == null) continue;
            for (int a = 0; a < data.faces[f].arrows.Count; a++)
            {
                var arrow = data.faces[f].arrows[a];
                // Head
                if (f == selectedFaceIndex)
                {
                    string headKey = $"{arrow.position.x},{arrow.position.y}";
                    headCellSet.Add(headKey);
                    cellToArrowIndex[headKey] = a;
                }
                // Body cells trên face hiện tại
                if (arrow.bodyCells != null)
                {
                    foreach (var bc in arrow.bodyCells)
                    {
                        if (bc.faceIndex == selectedFaceIndex)
                        {
                            string key = $"{bc.position.x},{bc.position.y}";
                            bodyCellSet.Add(key);
                        }
                    }
                }
            }
        }

        // Valid next positions (chỉ trên face hiện tại)
        HashSet<string> validSet = new HashSet<string>();
        if (validNexts != null)
        {
            foreach (var v in validNexts)
                if (v.face == selectedFaceIndex)
                    validSet.Add($"{v.pos.x},{v.pos.y}");
        }

        // Last body cell (có thể xoá bằng click)
        string lastCellKey = "";
        if (isDrawing && drawFace >= 0 && drawIndex >= 0)
        {
            var drawArrow = data.faces[drawFace].arrows[drawIndex];
            if (drawArrow.bodyCells != null && drawArrow.bodyCells.Count > 0)
            {
                var last = drawArrow.bodyCells[drawArrow.bodyCells.Count - 1];
                if (last.faceIndex == selectedFaceIndex)
                    lastCellKey = $"{last.position.x},{last.position.y}";
            }
        }

        // ======== VẼ LƯỚI ========
        EditorGUILayout.BeginVertical("box");
        for (int y = data.gridSize - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < data.gridSize; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                string key = $"{x},{y}";

                ArrowSpawnData existingArrow = (selectedFaceIndex < data.faces.Length)
                    ? currentFace.arrows.Find(a => a.position == pos) : null;

                bool isHead = headCellSet.Contains(key);
                bool isBody = bodyCellSet.Contains(key);
                bool isValid = validSet.Contains(key);
                bool isLast = (key == lastCellKey);

                // Chọn nhãn ô
                string btnText = " ";
                if (isHead && existingArrow != null)
                {
                    int di = GetDirIndex(existingArrow.initialDirection);
                    btnText = (di >= 0) ? arrows[di] : "?";
                    if (existingArrow.bodyCells != null && existingArrow.bodyCells.Count > 0)
                        btnText += "·";
                }
                else if (isBody)
                {
                    btnText = isLast ? "✕" : "█";
                }
                else if (isValid)
                {
                    btnText = "+";
                }

                // Chọn màu nền
                if (isHead)
                    GUI.backgroundColor = new Color(0.5f, 0.95f, 0.5f);
                else if (isLast)
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Đỏ = click để xoá
                else if (isBody)
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.5f); // Cam = thân
                else if (isValid)
                    GUI.backgroundColor = new Color(1f, 1f, 0.6f); // Vàng = click để thêm
                else
                    GUI.backgroundColor = Color.white;

                // ======== XỬ LÝ CLICK ========
                if (GUILayout.Button(btnText, GUILayout.Width(45), GUILayout.Height(45)))
                {
                    if (isDrawing)
                    {
                        // DRAW MODE
                        if (isValid)
                        {
                            // Thêm body cell
                            var drawArrow = data.faces[drawFace].arrows[drawIndex];
                            if (drawArrow.bodyCells == null) drawArrow.bodyCells = new List<BodyCell>();
                            drawArrow.bodyCells.Add(new BodyCell { faceIndex = selectedFaceIndex, position = pos });
                        }
                        else if (isLast)
                        {
                            // Xoá cell cuối (undo)
                            var drawArrow = data.faces[drawFace].arrows[drawIndex];
                            if (drawArrow.bodyCells != null && drawArrow.bodyCells.Count > 0)
                                drawArrow.bodyCells.RemoveAt(drawArrow.bodyCells.Count - 1);
                        }
                    }
                    else
                    {
                        // NORMAL MODE: đặt / xoay / xoá arrowhead
                        if (existingArrow == null)
                        {
                            currentFace.arrows.Add(new ArrowSpawnData
                            {
                                position = pos,
                                initialDirection = dirs[0],
                                bodyCells = new List<BodyCell>()
                            });
                        }
                        else
                        {
                            int ci = GetDirIndex(existingArrow.initialDirection);
                            ci++;
                            if (ci >= dirs.Length)
                                currentFace.arrows.Remove(existingArrow);
                            else
                                existingArrow.initialDirection = dirs[ci];
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // ======== BƯỚC 4: PANEL CHI TIẾT + NÚT VẼ ========
        EditorGUILayout.LabelField("BƯỚC 4: Chi Tiết Mũi Tên", EditorStyles.boldLabel);

        if (currentFace.arrows.Count == 0)
            EditorGUILayout.LabelField("Chưa có mũi tên. Hãy click lên lưới.", EditorStyles.miniLabel);

        for (int i = 0; i < currentFace.arrows.Count; i++)
        {
            ArrowSpawnData a = currentFace.arrows[i];
            int di = GetDirIndex(a.initialDirection);
            string icon = (di >= 0) ? arrows[di] : "?";
            int bodyCount = (a.bodyCells != null) ? a.bodyCells.Count : 0;
            bool isThisDrawing = isDrawing && drawFace == selectedFaceIndex && drawIndex == i;

            EditorGUILayout.BeginVertical("helpbox");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{icon} ({a.position.x},{a.position.y})  •  Thân: {bodyCount} ô", EditorStyles.boldLabel);

            // Nút vẽ / xong
            if (isThisDrawing)
            {
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("❌ Xong Vẽ", GUILayout.Width(80)))
                {
                    isDrawing = false;
                    drawFace = -1;
                    drawIndex = -1;
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.6f, 0.95f, 1f);
                if (GUILayout.Button("✏️ Vẽ Thân", GUILayout.Width(80)))
                {
                    isDrawing = true;
                    drawFace = selectedFaceIndex;
                    drawIndex = i;
                }
            }
            GUI.backgroundColor = Color.white;

            // Nút xoá toàn bộ thân
            if (bodyCount > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.85f, 0.85f);
                if (GUILayout.Button("🗑", GUILayout.Width(30)))
                {
                    a.bodyCells.Clear();
                    if (isThisDrawing) { isDrawing = false; drawFace = -1; drawIndex = -1; }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // Hiển thị danh sách body cells (chỉ khi đang vẽ arrow này)
            if (isThisDrawing && a.bodyCells != null && a.bodyCells.Count > 0)
            {
                EditorGUI.indentLevel++;
                for (int c = 0; c < a.bodyCells.Count; c++)
                {
                    var bc = a.bodyCells[c];
                    string faceName = data.faces[bc.faceIndex].faceName;
                    EditorGUILayout.LabelField($"  {c + 1}. Face {faceName} → ({bc.position.x},{bc.position.y})", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }

    // ====================================================================
    // TÍNH CÁC Ô HỢP LỆ ĐỂ VẼ TIẾP (bao gồm cross-face)
    // ====================================================================
    private List<(int face, Vector2Int pos)> GetValidNextCells(ArrowLevelData data)
    {
        var result = new List<(int, Vector2Int)>();
        if (!isDrawing || drawFace < 0 || drawIndex < 0) return result;
        if (drawFace >= data.faces.Length) return result;

        FaceData face = data.faces[drawFace];
        if (drawIndex >= face.arrows.Count) return result;

        ArrowSpawnData arrow = face.arrows[drawIndex];

        // Tìm ô cuối cùng
        int lastFace;
        Vector2Int lastPos;
        if (arrow.bodyCells != null && arrow.bodyCells.Count > 0)
        {
            var last = arrow.bodyCells[arrow.bodyCells.Count - 1];
            lastFace = last.faceIndex;
            lastPos = last.position;
        }
        else
        {
            lastFace = drawFace;
            lastPos = arrow.position;
        }

        // Kiểm tra 4 hướng liền kề
        Vector2Int[] adjDirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var dir in adjDirs)
        {
            Vector2Int nextPos = lastPos + dir;

            if (nextPos.x >= 0 && nextPos.x < data.gridSize && nextPos.y >= 0 && nextPos.y < data.gridSize)
            {
                // Cùng face, trong lưới
                if (!IsCellOccupied(data, arrow, drawFace, lastFace, nextPos))
                    result.Add((lastFace, nextPos));
            }
            // TODO: Cross-face drawing tạm tắt — cần recalibrate CubeTopology cho khớp rotation
            // else
            // {
            //     var (newFace, newPos, _) = CubeTopology.CrossEdge(lastFace, lastPos, dir, data.gridSize);
            //     if (!IsCellOccupied(data, arrow, drawFace, newFace, newPos))
            //         result.Add((newFace, newPos));
            // }
        }

        return result;
    }

    /// <summary>
    /// Kiểm tra ô đã bị chiếm bởi arrow hiện tại hoặc arrow khác
    /// </summary>
    private bool IsCellOccupied(ArrowLevelData data, ArrowSpawnData currentArrow, int currentArrowFace, int cellFace, Vector2Int cellPos)
    {
        // Kiểm tra đầu của chính nó
        if (currentArrowFace == cellFace && currentArrow.position == cellPos) return true;

        // Kiểm tra body của chính nó
        if (currentArrow.bodyCells != null)
        {
            foreach (var bc in currentArrow.bodyCells)
                if (bc.faceIndex == cellFace && bc.position == cellPos) return true;
        }

        // Kiểm tra các arrow khác trên face đó
        if (cellFace < data.faces.Length && data.faces[cellFace].arrows != null)
        {
            foreach (var other in data.faces[cellFace].arrows)
            {
                if (other == currentArrow) continue;
                if (other.position == cellPos) return true;
                if (other.bodyCells != null)
                {
                    foreach (var bc in other.bodyCells)
                        if (bc.faceIndex == cellFace && bc.position == cellPos) return true;
                }
            }
        }

        return false;
    }

    private void ValidateDrawState(ArrowLevelData data)
    {
        if (!isDrawing) return;
        if (drawFace < 0 || drawFace >= data.faces.Length ||
            drawIndex < 0 || drawIndex >= data.faces[drawFace].arrows.Count)
        {
            isDrawing = false;
            drawFace = -1;
            drawIndex = -1;
        }
    }

    private int GetDirIndex(Vector2Int dir)
    {
        for (int i = 0; i < dirs.Length; i++)
            if (dirs[i] == dir) return i;
        return -1;
    }
}
