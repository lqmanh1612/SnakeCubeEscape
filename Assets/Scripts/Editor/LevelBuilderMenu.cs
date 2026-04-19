using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;

public class LevelBuilderMenu : MonoBehaviour
{
    [MenuItem("Tools/Arrow Cube Escape/Generate Level from GameManager")]
    public static void GenerateCubeLevelFromMenu()
    {
        GenerateCubeLevel(null);
    }

    public static void GenerateCubeLevel(ArrowLevelData editorOverride = null)
    {
        GameManager gm = GameObject.FindObjectOfType<GameManager>();
        if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();

        ArrowLevelData levelData = editorOverride != null ? editorOverride : gm.currentLevelData;
        int gridSize = 5;
        if (levelData == null) Debug.LogWarning("Chưa gán currentLevelData trong GameManager!");
        else gridSize = levelData.gridSize;

        FaceGrid.ClearAllFaces();

        StructurePivot pivot = GameObject.FindObjectOfType<StructurePivot>();
        if (pivot == null) pivot = new GameObject("StructurePivot").AddComponent<StructurePivot>();
        pivot.transform.position = Vector3.zero;
        pivot.transform.rotation = Quaternion.identity;
        while (pivot.transform.childCount > 0) DestroyImmediate(pivot.transform.GetChild(0).gameObject);

        // ======== 1. KHỐI KÍNH ========
        float coreSize = gridSize * 1f;
        GameObject coreCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coreCube.name = "CentralCube_Visual";
        coreCube.transform.localScale = Vector3.one * coreSize * 0.98f;
        coreCube.transform.SetParent(pivot.transform);
        coreCube.transform.localPosition = Vector3.zero;
        DestroyImmediate(coreCube.GetComponent<Collider>()); // Xoá collider để raycast xuyên qua kính

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.82f, 0.88f, 0.95f);
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
        coreCube.GetComponent<MeshRenderer>().material = CreateSafeMaterial(new Color(1f, 1f, 1f, 0.85f));

        // ======== 2. SINH 6 MẶT ========
        float fo = coreSize / 2f;
        Vector3[] fPos = {
            new Vector3(0,0,-fo), new Vector3(0,0,fo),
            new Vector3(-fo,0,0), new Vector3(fo,0,0),
            new Vector3(0,fo,0),  new Vector3(0,-fo,0)
        };
        Vector3[] fRot = {
            new Vector3(0,180,0), new Vector3(0,0,0),
            new Vector3(0,-90,0), new Vector3(0,90,0),
            new Vector3(-90,0,0), new Vector3(90,0,0)
        };

        Material inkMat = new Material(Shader.Find("Unlit/Color"));
        inkMat.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Material headMat = new Material(Shader.Find("Unlit/Color"));
        headMat.color = new Color(0.0f, 0.75f, 0.85f, 1f); // Cyan cho đầu mũi tên

        Material outlineMat = new Material(Shader.Find("Unlit/Color"));
        outlineMat.color = new Color(0.95f, 0.15f, 0.15f, 1f); // Đỏ cho viền bị chặn

        FaceGrid[] grids = new FaceGrid[6];
        for (int i = 0; i < 6; i++)
        {
            GameObject faceObj = new GameObject($"Face_{i}");
            faceObj.transform.SetParent(pivot.transform);
            faceObj.transform.localPosition = fPos[i];
            faceObj.transform.localEulerAngles = fRot[i];
            FaceGrid grid = faceObj.AddComponent<FaceGrid>();
            grid.faceIndex = i;
            grid.gridWidth = gridSize;
            grid.gridHeight = gridSize;
            grid.cellSize = 1f;
            grids[i] = grid;
        }

        // ======== 3. VẼ MŨI TÊN ========
        if (levelData != null)
        {
            for (int f = 0; f < 6; f++)
            {
                if (f < levelData.faces.Length && levelData.faces[f].arrows != null)
                {
                    foreach (var arrData in levelData.faces[f].arrows)
                        BuildArrow(grids, f, arrData, inkMat, headMat, outlineMat);
                }
            }
        }

        // ======== 4. CAMERA ========
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(coreSize + 2, coreSize + 2, -coreSize - 2);
            Camera.main.transform.LookAt(Vector3.zero);
        }
        Debug.Log("✅ Ink Arrows Generated!");
    }

    private static void BuildArrow(FaceGrid[] grids, int faceIndex, ArrowSpawnData data, Material inkMat, Material headMat, Material outlineMat)
    {
        FaceGrid homeGrid = grids[faceIndex];
        List<BodyCell> allCells = data.GetAllCells(faceIndex);

        GameObject arrowRoot = new GameObject($"Arrow_F{faceIndex}_{data.position.x}_{data.position.y}");
        arrowRoot.transform.SetParent(homeGrid.transform);
        arrowRoot.transform.localPosition = homeGrid.GetLocalPosition(data.position);

        ArrowTile tile = arrowRoot.AddComponent<ArrowTile>();
        tile.Setup(homeGrid, data, allCells, faceIndex);

        // Tạo visual segments + outline riêng biệt cho từng cell
        List<Transform> segments = new List<Transform>();
        List<GameObject> outlines = new List<GameObject>();
        DrawFlatInkSegmented(arrowRoot, grids, allCells, data.initialDirection,
                             faceIndex, inkMat, headMat, outlineMat, segments, outlines);
        tile.segments = segments;
        tile.outlineObjects = outlines;

        // Auto-size collider trên root (cho Raycast tap)
        BoxCollider bc = arrowRoot.AddComponent<BoxCollider>();
        if (allCells.Count > 1)
        {
            Vector3 headLocal = homeGrid.GetLocalPosition(data.position);
            Vector3 minP = headLocal, maxP = headLocal;
            foreach (var c in allCells)
            {
                if (c.faceIndex != faceIndex) continue;
                Vector3 p = homeGrid.GetLocalPosition(c.position);
                minP = Vector3.Min(minP, p);
                maxP = Vector3.Max(maxP, p);
            }
            bc.center = (minP + maxP) / 2f - headLocal;
            Vector3 sz = maxP - minP + Vector3.one * 0.8f;
            sz.z = 0.5f;
            bc.size = sz;
        }
        else
        {
            bc.center = Vector3.zero;
            bc.size = new Vector3(0.8f, 0.8f, 0.5f);
        }
    }

    // ====================================================================
    // VẼ NÉT MỰC — SEGMENTED (mỗi cell = 1 segment riêng cho snake anim)
    // ====================================================================
    private static void DrawFlatInkSegmented(GameObject arrowRoot, FaceGrid[] grids,
        List<BodyCell> allCells, Vector2Int flightDir, int headFaceIndex,
        Material inkMat, Material headMat, Material outlineMat,
        List<Transform> outSegments, List<GameObject> outOutlines)
    {
        if (allCells.Count == 0) return;

        float lineWidth = 0.13f;
        float lineDepth = 0.02f;
        float surfOff = 0.015f;
        float cellSize = grids[0].cellSize;
        Transform root = arrowRoot.transform;
        float outlineScale = 1.25f; // Viền lớn hơn 25%

        for (int i = 0; i < allCells.Count; i++)
        {
            BodyCell cell = allCells[i];
            FaceGrid face = grids[cell.faceIndex];
            Vector3 fN = face.transform.forward;
            Vector3 wPos = face.GetWorldPosition(cell.position) + fN * surfOff;

            // === Container segment cho cell này ===
            GameObject seg = new GameObject($"Segment_{i}");
            seg.transform.position = wPos;
            seg.transform.rotation = face.transform.rotation;
            seg.transform.SetParent(root, true);

            // === Nút khối vuông đại diện ô (FIX: localRotation = identity) ===
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(seg.transform);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.identity;
            
            // Thu nhỏ phần thân (i > 0) đi 1 nửa để đầu to hơn
            float bodySize = (i == 0) ? (cellSize * 0.65f) : (cellSize * 0.4f);
            vis.transform.localScale = new Vector3(bodySize, bodySize, lineDepth);
            vis.GetComponent<MeshRenderer>().material = (i == 0) ? headMat : inkMat;

            // === Đường viền đỏ (ẩn mặc định, hiện khi bị chặn) ===
            GameObject outline = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(outline.GetComponent<Collider>());
            outline.name = "Outline";
            outline.transform.SetParent(seg.transform);
            outline.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = new Vector3(bodySize * outlineScale, bodySize * outlineScale, lineDepth * 0.5f);
            outline.GetComponent<MeshRenderer>().material = outlineMat;
            outline.SetActive(false);
            outOutlines.Add(outline);

            // === Nếu là Head (i==0): thêm chóp mũi tên ===
            if (i == 0)
            {
                float flyDirX = flightDir.x;
                float flyDirY = flightDir.y;
                if (cell.faceIndex < 4) flyDirX = -flyDirX;
                else if (cell.faceIndex == 5) flyDirY = -flyDirY;
                Vector3 dW = face.transform.TransformDirection(
                    new Vector3(flyDirX, flyDirY, 0f)).normalized;
                Vector3 tip = wPos + dW * (cellSize * 0.35f);

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(head.GetComponent<Collider>());
                head.transform.SetParent(seg.transform);
                head.transform.position = tip;
                head.transform.rotation = Quaternion.LookRotation(dW, fN)
                                          * Quaternion.Euler(0, 0, 45f);
                float ts = lineWidth * 2.2f;
                head.transform.localScale = new Vector3(ts, lineDepth, ts);
                head.GetComponent<MeshRenderer>().material = headMat;
            }

            // === Nét nối tới cell tiếp theo (i+1) ===
            if (i < allCells.Count - 1)
            {
                BodyCell nextCell = allCells[i + 1];
                if (nextCell.faceIndex == cell.faceIndex)
                {
                    // --- Cùng mặt: 1 đoạn thẳng ---
                    Vector3 wNext = face.GetWorldPosition(nextCell.position) + fN * surfOff;
                    float dist = Vector3.Distance(wPos, wNext);
                    if (dist > 0.01f)
                    {
                        Vector3 center = (wPos + wNext) / 2f;
                        Vector3 dir = (wNext - wPos).normalized;

                        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        DestroyImmediate(line.GetComponent<Collider>());
                        line.transform.SetParent(seg.transform);
                        line.transform.position = center;
                        line.transform.rotation = Quaternion.LookRotation(dir, fN);
                        line.transform.localScale = new Vector3(lineWidth, lineDepth, dist + lineWidth * 0.4f);
                        line.GetComponent<MeshRenderer>().material = inkMat;
                    }
                }
                else
                {
                    // --- Khác mặt: 2 đoạn gặp nhau ở cạnh khối ---
                    FaceGrid nextFace = grids[nextCell.faceIndex];
                    Vector3 nextFN = nextFace.transform.forward;
                    Vector3 wNext = nextFace.GetWorldPosition(nextCell.position) + nextFN * surfOff;

                    // Tính điểm góc (cube edge) thật sự nhờ giao tuyến của 2 mặt vuông góc
                    Vector3 trueEdge = Vector3.zero;
                    float nAx = Mathf.Abs(Mathf.Round(fN.x));
                    float nAy = Mathf.Abs(Mathf.Round(fN.y));
                    float nAz = Mathf.Abs(Mathf.Round(fN.z));
                    float nBx = Mathf.Abs(Mathf.Round(nextFN.x));
                    float nBy = Mathf.Abs(Mathf.Round(nextFN.y));
                    float nBz = Mathf.Abs(Mathf.Round(nextFN.z));

                    trueEdge.x = (nAx > 0.5f) ? wPos.x : (nBx > 0.5f) ? wNext.x : (wPos.x + wNext.x) / 2f;
                    trueEdge.y = (nAy > 0.5f) ? wPos.y : (nBy > 0.5f) ? wNext.y : (wPos.y + wNext.y) / 2f;
                    trueEdge.z = (nAz > 0.5f) ? wPos.z : (nBz > 0.5f) ? wNext.z : (wPos.z + wNext.z) / 2f;

                    Vector3 edgeMid = trueEdge;

                    // Đoạn 1: cell hiện tại → cạnh (trên mặt hiện tại)
                    float dist1 = Vector3.Distance(wPos, edgeMid);
                    if (dist1 > 0.01f)
                    {
                        Vector3 center1 = (wPos + edgeMid) / 2f;
                        Vector3 dir1 = (edgeMid - wPos).normalized;

                        GameObject line1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        DestroyImmediate(line1.GetComponent<Collider>());
                        line1.name = "CrossEdge_A";
                        line1.transform.SetParent(seg.transform);
                        line1.transform.position = center1;
                        line1.transform.rotation = Quaternion.LookRotation(dir1, fN);
                        line1.transform.localScale = new Vector3(lineWidth, lineDepth, dist1 + lineWidth * 0.2f);
                        line1.GetComponent<MeshRenderer>().material = inkMat;
                    }

                    // Đoạn 2: cạnh → cell tiếp theo (trên mặt tiếp theo)
                    // Sẽ được gán vào segment tiếp theo (i+1) ở vòng lặp sau,
                    // nhưng ta tạo ngay ở đây gán vào seg hiện tại để đảm bảo visual
                    float dist2 = Vector3.Distance(edgeMid, wNext);
                    if (dist2 > 0.01f)
                    {
                        Vector3 center2 = (edgeMid + wNext) / 2f;
                        Vector3 dir2 = (wNext - edgeMid).normalized;

                        GameObject line2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        DestroyImmediate(line2.GetComponent<Collider>());
                        line2.name = "CrossEdge_B";
                        line2.transform.SetParent(seg.transform);
                        line2.transform.position = center2;
                        line2.transform.rotation = Quaternion.LookRotation(dir2, nextFN);
                        line2.transform.localScale = new Vector3(lineWidth, lineDepth, dist2 + lineWidth * 0.2f);
                        line2.GetComponent<MeshRenderer>().material = inkMat;
                    }
                }
            }

            outSegments.Add(seg.transform);
        }
    }

    private static Material CreateSafeMaterial(Color color)
    {
        Material mat;
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp != null)
        {
            mat = new Material(urp);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return mat;
        }
        Shader std = Shader.Find("Standard");
        if (std != null)
        {
            mat = new Material(std);
            mat.color = color;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            return mat;
        }
        mat = new Material(Shader.Find("UI/Default"));
        mat.color = color;
        return mat;
    }
}
#endif
