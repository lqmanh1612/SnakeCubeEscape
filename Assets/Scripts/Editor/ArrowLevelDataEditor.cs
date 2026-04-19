#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom Editor cho ArrowLevelData — workflow hoàn toàn click-based:
///   1. Bấm [＋ Mũi Tên Mới] → click ô đặt đầu
///   2. Bấm hướng bay (4 nút ↑↓←→)
///   3. Click dấu ＋ xung quanh đuôi để vẽ thân → ＋ tự hiện ở mặt kế nếu đến mép
/// Hiện 6 mặt cùng lúc (cube net) để thấy cross-face liền mạch.
/// </summary>
[CustomEditor(typeof(ArrowLevelData))]
public class ArrowLevelDataEditor : Editor
{
    private enum State { Idle, PlacingHead, ChoosingDir, DrawingBody }

    private struct Candidate { public int face; public Vector2Int pos; }

    // Cube net layout: [row, col, faceIndex]
    //          col0   col1    col2    col3
    // row0:           Top(4)
    // row1: Left(2)  Front(0) Right(3) Back(1)
    // row2:          Bottom(5)
    private static readonly int[,] NET = {
        {0,1,4}, {1,0,2}, {1,1,0}, {1,2,3}, {1,3,1}, {2,1,5}
    };
    private static readonly string[] FN = {"Front","Back","Left","Right","Top","Bottom"};

    // Colors
    private static readonly Color C_HEAD  = new Color(0f, 0.75f, 0.85f);
    private static readonly Color C_BODY  = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color C_CROSS = new Color(0.6f, 0.15f, 0.7f, 0.9f);
    private static readonly Color C_EMPTY = new Color(0.93f, 0.93f, 0.93f);
    private static readonly Color C_GRID  = new Color(0.72f, 0.72f, 0.72f, 0.5f);
    private static readonly Color C_SEL   = new Color(1f, 0.85f, 0.1f);
    private static readonly Color C_PLUS  = new Color(0.15f, 0.78f, 0.3f, 0.75f);
    private static readonly Color C_DIR   = new Color(1f, 0.45f, 0.1f, 0.85f);
    private static readonly Color C_PLACE = new Color(0.4f, 0.75f, 1f, 0.25f);
    private static readonly Color C_FLBL  = new Color(0.3f, 0.3f, 0.3f, 0.75f);

    // State
    private State state = State.Idle;
    private int selF = -1, selA = -1;
    private List<Candidate> expand = new List<Candidate>();
    private Vector2 scroll;

    // ================================================================
    public override void OnInspectorGUI()
    {
        ArrowLevelData ld = (ArrowLevelData)target;

        EditorGUILayout.LabelField("⚙️ Cấu Hình", EditorStyles.boldLabel);
        ld.gridSize = EditorGUILayout.IntSlider("Grid Size", ld.gridSize, 3, 10);
        EditorGUILayout.Space(5);

        if (ld.faces == null || ld.faces.Length != 6) { EditorGUILayout.HelpBox("faces chưa khởi tạo!", MessageType.Error); return; }

        // State bar
        DrawStateBar(ld);
        EditorGUILayout.Space(3);

        // Cube Net
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawCubeNet(ld);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        DrawArrowList(ld);

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUILayout.Button("🔨 Generate Level", GUILayout.Height(30)))
            LevelBuilderMenu.GenerateCubeLevel(ld);
        GUI.backgroundColor = Color.white;

        if (GUI.changed) EditorUtility.SetDirty(target);
    }

    // ================================================================
    // STATE BAR
    // ================================================================
    private void DrawStateBar(ArrowLevelData ld)
    {
        switch (state)
        {
            case State.PlacingHead:
                EditorGUILayout.HelpBox("🎯 Click vào ô trống trên BẤT KỲ mặt nào để đặt ĐẦU mũi tên", MessageType.Warning);
                break;
            case State.ChoosingDir:
                EditorGUILayout.HelpBox("🧭 Chọn HƯỚNG BAY cho đầu mũi tên:", MessageType.Warning);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (DirBtn("↑ Lên", 0, 1, ld)) { }
                if (DirBtn("↓ Xuống", 0, -1, ld)) { }
                if (DirBtn("← Trái", -1, 0, ld)) { }
                if (DirBtn("→ Phải", 1, 0, ld)) { }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                break;
            case State.DrawingBody:
                EditorGUILayout.HelpBox(
                    "🖌️ Click dấu ＋ (xanh lá) xung quanh đuôi để nối thân.\n" +
                    "Khi đến mép mặt → ＋ tự hiện ở mặt kế tiếp!", MessageType.Info);
                break;
            default:
                EditorGUILayout.HelpBox("Bấm [＋ Mũi Tên Mới] hoặc click vào đầu (H) trên lưới để chọn", MessageType.None);
                break;
        }

        if (state != State.Idle)
        {
            GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
            if (GUILayout.Button("✕ Hủy / Xong", GUILayout.Height(22)))
            {
                // Nếu đang PlacingHead mà chưa đặt → xóa arrow rỗng nếu có
                state = State.Idle;
                expand.Clear();
                Repaint();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private bool DirBtn(string label, int dx, int dy, ArrowLevelData ld)
    {
        GUI.backgroundColor = C_DIR;
        bool clicked = GUILayout.Button(label, GUILayout.Width(70), GUILayout.Height(28));
        GUI.backgroundColor = Color.white;
        if (clicked && selF >= 0 && selA >= 0)
        {
            ld.faces[selF].arrows[selA].initialDirection = new Vector2Int(dx, dy);
            state = State.DrawingBody;
            RefreshExpand(ld);
            EditorUtility.SetDirty(target);
            Repaint();
        }
        return clicked;
    }

    // ================================================================
    // CUBE NET
    // ================================================================
    private float CellPx(int gs)
    {
        return Mathf.Clamp((EditorGUIUtility.currentViewWidth - 50) / (gs * 4 + 4), 14f, 28f);
    }

    private void DrawCubeNet(ArrowLevelData ld)
    {
        int gs = ld.gridSize;
        float px = CellPx(gs);
        float fp = px * gs;
        float gap = 3f;
        float h = 3 * (fp + gap + 14) + 10;

        Rect area = GUILayoutUtility.GetRect(4 * (fp + gap), h);

        for (int n = 0; n < 6; n++)
        {
            int row = NET[n, 0], col = NET[n, 1], fi = NET[n, 2];
            float fx = area.x + col * (fp + gap);
            float fy = area.y + row * (fp + gap + 14);

            // Label
            Rect lr = new Rect(fx, fy, fp, 13);
            EditorGUI.DrawRect(lr, C_FLBL);
            GUI.Label(lr, $"{fi}:{FN[fi]}", LabelStyle(Color.white, 9, FontStyle.Bold));

            // Face bg
            Rect fr = new Rect(fx, fy + 14, fp, fp);
            EditorGUI.DrawRect(fr, C_EMPTY);

            // Placeable highlight
            if (state == State.PlacingHead)
                EditorGUI.DrawRect(fr, C_PLACE);

            DrawFace(ld, fi, fr, gs, px);

            // Grid lines
            for (int x = 0; x <= gs; x++)
                EditorGUI.DrawRect(new Rect(fr.x + x * px, fr.y, 1, fp), C_GRID);
            for (int y = 0; y <= gs; y++)
                EditorGUI.DrawRect(new Rect(fr.x, fr.y + y * px, fp, 1), C_GRID);

            HandleClick(ld, fi, fr, gs, px);
        }
    }

    // ================================================================
    // DRAW FACE CONTENT
    // ================================================================
    private void DrawFace(ArrowLevelData ld, int fi, Rect fr, int gs, float px)
    {
        // 1. Draw all arrows' cells visible on this face
        for (int f = 0; f < 6; f++)
        {
            if (ld.faces[f].arrows == null) continue;
            for (int a = 0; a < ld.faces[f].arrows.Count; a++)
            {
                var arr = ld.faces[f].arrows[a];
                bool sel = (f == selF && a == selA);
                float alpha = sel ? 1f : 0.35f;

                // Head
                if (f == fi)
                {
                    Color hc = WithAlpha(C_HEAD, alpha);
                    CellDraw(fr, gs, px, arr.position, hc, sel, "H");
                    if (sel && state != State.ChoosingDir)
                        DirSymbol(fr, gs, px, arr.position, arr.initialDirection);
                }

                // Body cells on this face
                for (int b = 0; b < arr.bodyCells.Count; b++)
                {
                    var bc = arr.bodyCells[b];
                    if (bc.faceIndex != fi) continue;
                    bool cross = (f != fi);
                    Color c = sel ? (cross ? C_CROSS : C_BODY) : WithAlpha(C_BODY, 0.3f);
                    string lbl = cross ? $"→{b + 1}" : $"{b + 1}";
                    CellDraw(fr, gs, px, bc.position, c, sel, lbl);
                }
            }
        }

        // 2. Expand candidates (＋ markers)
        if (state == State.DrawingBody)
        {
            foreach (var c in expand)
            {
                if (c.face != fi) continue;
                CellDraw(fr, gs, px, c.pos, C_PLUS, false, "＋");
            }
        }

        // 3. Direction chooser markers on grid (visual aid — actual choosing via buttons)
        if (state == State.ChoosingDir && selF == fi && selF >= 0 && selA >= 0)
        {
            var arr = ld.faces[selF].arrows[selA];
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            string[] syms = { "▲", "▼", "◄", "►" };
            for (int d = 0; d < 4; d++)
            {
                Vector2Int adj = arr.position + dirs[d];
                if (adj.x >= 0 && adj.x < gs && adj.y >= 0 && adj.y < gs)
                    CellDraw(fr, gs, px, adj, C_DIR, false, syms[d]);
            }
        }
    }

    // ================================================================
    // CLICK HANDLER
    // ================================================================
    private void HandleClick(ArrowLevelData ld, int fi, Rect fr, int gs, float px)
    {
        Event e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0 || !fr.Contains(e.mousePosition)) return;

        Vector2 loc = e.mousePosition - fr.position;
        int cx = Mathf.Clamp(Mathf.FloorToInt(loc.x / px), 0, gs - 1);
        int cy = Mathf.Clamp(gs - 1 - Mathf.FloorToInt(loc.y / px), 0, gs - 1);
        Vector2Int pos = new Vector2Int(cx, cy);

        switch (state)
        {
            case State.Idle:
                TrySelect(ld, fi, pos);
                break;
            case State.PlacingHead:
                DoPlaceHead(ld, fi, pos);
                break;
            case State.ChoosingDir:
                // Hướng chọn qua buttons, nhưng cũng hỗ trợ click adjacent cell
                TryClickDir(ld, fi, pos);
                break;
            case State.DrawingBody:
                TryExpand(ld, fi, pos);
                break;
        }
        e.Use();
        Repaint();
    }

    private void TrySelect(ArrowLevelData ld, int fi, Vector2Int pos)
    {
        var face = ld.faces[fi];
        if (face.arrows != null)
        {
            for (int a = 0; a < face.arrows.Count; a++)
            {
                if (face.arrows[a].position == pos)
                { selF = fi; selA = a; return; }
            }
        }
        selF = -1; selA = -1;
    }

    private void DoPlaceHead(ArrowLevelData ld, int fi, Vector2Int pos)
    {
        if (Occupied(ld, fi, pos, null)) return;

        var face = ld.faces[fi];
        if (face.arrows == null) face.arrows = new List<ArrowSpawnData>();
        face.arrows.Add(new ArrowSpawnData
        {
            position = pos,
            initialDirection = Vector2Int.up
        });
        selF = fi;
        selA = face.arrows.Count - 1;
        state = State.ChoosingDir;
        EditorUtility.SetDirty(target);
    }

    private void TryClickDir(ArrowLevelData ld, int fi, Vector2Int pos)
    {
        if (selF < 0 || fi != selF) return;
        var arr = ld.faces[selF].arrows[selA];
        Vector2Int diff = pos - arr.position;
        if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) != 1) return;

        arr.initialDirection = diff;
        state = State.DrawingBody;
        RefreshExpand(ld);
        EditorUtility.SetDirty(target);
    }

    private void TryExpand(ArrowLevelData ld, int fi, Vector2Int pos)
    {
        foreach (var c in expand)
        {
            if (c.face == fi && c.pos == pos)
            {
                ld.faces[selF].arrows[selA].bodyCells.Add(
                    new BodyCell { faceIndex = fi, position = pos });
                RefreshExpand(ld);
                EditorUtility.SetDirty(target);
                return;
            }
        }
    }

    // ================================================================
    // EXPAND CANDIDATES — cross-face via CubeTopology
    // ================================================================
    private void RefreshExpand(ArrowLevelData ld)
    {
        expand.Clear();
        if (selF < 0 || selA < 0) return;
        var arr = ld.faces[selF].arrows[selA];
        int gs = ld.gridSize;

        // Tail = last body cell or head
        int tf; Vector2Int tp;
        if (arr.bodyCells.Count > 0)
        {
            var last = arr.bodyCells[arr.bodyCells.Count - 1];
            tf = last.faceIndex; tp = last.position;
        }
        else { tf = selF; tp = arr.position; }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in dirs)
        {
            Vector2Int np = tp + d;
            int nf = tf;

            if (np.x < 0 || np.x >= gs || np.y < 0 || np.y >= gs)
            {
                // === Cross-face ===
                var (cf, cp, _) = CubeTopology.CrossEdge(tf, tp, d, gs);
                nf = cf; np = cp;
            }

            // Skip self head
            if (nf == selF && np == arr.position) continue;
            // Skip existing body cells
            bool dup = false;
            foreach (var bc in arr.bodyCells)
                if (bc.faceIndex == nf && bc.position == np) { dup = true; break; }
            if (dup) continue;
            // Skip other arrows
            if (Occupied(ld, nf, np, arr)) continue;

            expand.Add(new Candidate { face = nf, pos = np });
        }
    }

    // ================================================================
    // COLLISION CHECK
    // ================================================================
    private bool Occupied(ArrowLevelData ld, int face, Vector2Int pos, ArrowSpawnData exclude)
    {
        for (int f = 0; f < 6; f++)
        {
            if (ld.faces[f].arrows == null) continue;
            foreach (var a in ld.faces[f].arrows)
            {
                if (a == exclude) continue;
                if (f == face && a.position == pos) return true;
                foreach (var bc in a.bodyCells)
                    if (bc.faceIndex == face && bc.position == pos) return true;
            }
        }
        return false;
    }

    // ================================================================
    // ARROW LIST
    // ================================================================
    private void DrawArrowList(ArrowLevelData ld)
    {
        EditorGUILayout.LabelField("📋 Mũi Tên", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("＋ Mũi Tên Mới", GUILayout.Height(26)))
        {
            state = State.PlacingHead;
            selF = -1; selA = -1;
            expand.Clear();
            Repaint();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(3);

        for (int f = 0; f < 6; f++)
        {
            var face = ld.faces[f];
            if (face.arrows == null || face.arrows.Count == 0) continue;

            EditorGUILayout.LabelField($"  {FN[f]} ({face.arrows.Count})", EditorStyles.miniLabel);

            for (int a = 0; a < face.arrows.Count; a++)
            {
                var arr = face.arrows[a];
                bool sel = (f == selF && a == selA);

                EditorGUILayout.BeginHorizontal();

                // Select toggle
                GUI.backgroundColor = sel ? C_SEL : Color.white;
                if (GUILayout.Button(sel ? "●" : "○", GUILayout.Width(20)))
                {
                    if (sel) { selF = -1; selA = -1; state = State.Idle; expand.Clear(); }
                    else { selF = f; selA = a; state = State.Idle; expand.Clear(); }
                    Repaint();
                }
                GUI.backgroundColor = Color.white;

                // Info
                string ds = DirSym(arr.initialDirection);
                int crossN = 0;
                foreach (var bc in arr.bodyCells) if (bc.faceIndex != f) crossN++;
                string bodyTxt = crossN > 0 ? $"{arr.bodyCells.Count}({crossN}⨯)" : $"{arr.bodyCells.Count}";
                EditorGUILayout.LabelField($"({arr.position.x},{arr.position.y}) {ds} thân:{bodyTxt}", GUILayout.Width(155));

                // Draw body button
                if (sel)
                {
                    bool drawing = state == State.DrawingBody;
                    GUI.backgroundColor = drawing ? new Color(1f, 0.35f, 0.35f) : new Color(0.3f, 0.9f, 0.4f);
                    if (GUILayout.Button(drawing ? "⏹" : "🖌️", GUILayout.Width(28)))
                    {
                        if (drawing) { state = State.Idle; expand.Clear(); }
                        else { state = State.DrawingBody; RefreshExpand(ld); }
                        Repaint();
                    }
                    GUI.backgroundColor = Color.white;
                }

                // Delete
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("✕", GUILayout.Width(20)))
                {
                    face.arrows.RemoveAt(a);
                    if (sel) { selF = -1; selA = -1; state = State.Idle; expand.Clear(); }
                    EditorUtility.SetDirty(target);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // Body detail
                if (sel && arr.bodyCells.Count > 0)
                {
                    EditorGUI.indentLevel += 2;
                    for (int b = 0; b < arr.bodyCells.Count; b++)
                    {
                        var bc = arr.bodyCells[b];
                        bool cross = bc.faceIndex != f;
                        EditorGUILayout.BeginHorizontal();
                        string fn = cross ? $" [{FN[bc.faceIndex]}]" : "";
                        EditorGUILayout.LabelField($"  {b + 1}: ({bc.position.x},{bc.position.y}){fn}", GUILayout.Width(170));
                        if (GUILayout.Button("✂", GUILayout.Width(22)))
                        {
                            arr.bodyCells.RemoveRange(b, arr.bodyCells.Count - b);
                            if (state == State.DrawingBody) RefreshExpand(ld);
                            EditorUtility.SetDirty(target);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel -= 2;
                }
            }
        }
    }

    // ================================================================
    // DRAWING HELPERS
    // ================================================================
    private void CellDraw(Rect fr, int gs, float px, Vector2Int pos, Color col, bool sel, string lbl)
    {
        if (pos.x < 0 || pos.x >= gs || pos.y < 0 || pos.y >= gs) return;
        Rect cr = new Rect(fr.x + pos.x * px + 1, fr.y + (gs - 1 - pos.y) * px + 1, px - 2, px - 2);
        EditorGUI.DrawRect(cr, col);

        if (sel)
        {
            float b = Mathf.Max(1, px * 0.09f);
            EditorGUI.DrawRect(new Rect(cr.x, cr.y, cr.width, b), C_SEL);
            EditorGUI.DrawRect(new Rect(cr.x, cr.yMax - b, cr.width, b), C_SEL);
            EditorGUI.DrawRect(new Rect(cr.x, cr.y, b, cr.height), C_SEL);
            EditorGUI.DrawRect(new Rect(cr.xMax - b, cr.y, b, cr.height), C_SEL);
        }
        if (px >= 14f)
            GUI.Label(cr, lbl, LabelStyle(Color.white, Mathf.Clamp((int)(px * 0.38f), 7, 13), FontStyle.Normal));
    }

    private void DirSymbol(Rect fr, int gs, float px, Vector2Int pos, Vector2Int dir)
    {
        if (pos.x < 0 || pos.x >= gs || pos.y < 0 || pos.y >= gs) return;
        Rect cr = new Rect(fr.x + pos.x * px + 1, fr.y + (gs - 1 - pos.y) * px + 1, px - 2, px - 2);
        GUI.Label(cr, DirSym(dir), LabelStyle(C_SEL, Mathf.Clamp((int)(px * 0.55f), 9, 16), FontStyle.Bold));
    }

    private static string DirSym(Vector2Int d)
    {
        if (d == Vector2Int.up) return "↑";
        if (d == Vector2Int.down) return "↓";
        if (d == Vector2Int.left) return "←";
        return "→";
    }

    private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private static GUIStyle LabelStyle(Color c, int size, FontStyle fs)
    {
        return new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = c },
            fontSize = size,
            fontStyle = fs
        };
    }
}
#endif
