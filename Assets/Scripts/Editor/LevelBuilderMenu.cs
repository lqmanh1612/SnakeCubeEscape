using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;

public class LevelBuilderMenu : MonoBehaviour
{
    [MenuItem("Tools/Arrow Cube Escape/Generate Level from GameManager")]
    public static void GenerateCubeLevel()
    {
        GameManager gm = GameObject.FindObjectOfType<GameManager>();
        if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();

        ArrowLevelData levelData = gm.currentLevelData;
        int gridSize = 5;
        if (levelData == null) Debug.LogWarning("Chưa gán currentLevelData trong GameManager!");
        else gridSize = levelData.gridSize;

        FaceGrid.ClearAllFaces();

        StructurePivot pivot = GameObject.FindObjectOfType<StructurePivot>();
        if (pivot == null) pivot = new GameObject("StructurePivot").AddComponent<StructurePivot>();
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
                        BuildArrow(grids, f, arrData, inkMat);
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

    private static void BuildArrow(FaceGrid[] grids, int faceIndex, ArrowSpawnData data, Material inkMat)
    {
        FaceGrid homeGrid = grids[faceIndex];
        List<BodyCell> allCells = data.GetAllCells(faceIndex);

        GameObject arrowRoot = new GameObject($"Arrow_F{faceIndex}_{data.position.x}_{data.position.y}");
        arrowRoot.transform.SetParent(homeGrid.transform);
        arrowRoot.transform.localPosition = homeGrid.GetLocalPosition(data.position);

        ArrowTile tile = arrowRoot.AddComponent<ArrowTile>();
        tile.Setup(homeGrid, data, allCells, faceIndex);

        DrawFlatInk(arrowRoot, grids, allCells, data.initialDirection, faceIndex, inkMat);

        // Auto-size collider
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
    // VẼ NÉT MỰC PHẲNG (hỗ trợ cross-face)
    // ====================================================================
    private static void DrawFlatInk(GameObject arrowRoot, FaceGrid[] grids, List<BodyCell> allCells, 
        Vector2Int flightDir, int headFaceIndex, Material inkMat)
    {
        if (allCells.Count == 0) return;

        float lineWidth = 0.13f;
        float lineDepth = 0.02f;
        float surfOff = 0.015f;
        Transform root = arrowRoot.transform;

        // Vẽ nét nối giữa từng cặp ô liên tiếp
        for (int i = 0; i < allCells.Count - 1; i++)
        {
            BodyCell a = allCells[i];
            BodyCell b = allCells[i + 1];

            // Bỏ qua đoạn xuyên cạnh (khe gập 90° tự nhiên nối)
            if (a.faceIndex != b.faceIndex) continue;

            FaceGrid face = grids[a.faceIndex];
            Vector3 fN = face.transform.forward;
            Vector3 wA = face.GetWorldPosition(a.position);
            Vector3 wB = face.GetWorldPosition(b.position);
            float dist = Vector3.Distance(wA, wB);
            if (dist < 0.01f) continue;

            Vector3 center = (wA + wB) / 2f + fN * surfOff;
            Vector3 dir = (wB - wA).normalized;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(seg.GetComponent<Collider>());
            seg.transform.SetParent(root);
            seg.transform.position = center;
            seg.transform.rotation = Quaternion.LookRotation(dir, fN);
            seg.transform.localScale = new Vector3(lineWidth, lineDepth, dist + lineWidth * 0.4f);
            seg.GetComponent<MeshRenderer>().material = inkMat;
        }

        // Chóp mũi tên
        BodyCell headCell = allCells[0];
        FaceGrid hFace = grids[headCell.faceIndex];
        Vector3 hN = hFace.transform.forward;
        Vector3 hW = hFace.GetWorldPosition(headCell.position) + hN * surfOff;
        Vector3 dW = hFace.transform.TransformDirection(new Vector3(flightDir.x, flightDir.y, 0f)).normalized;
        Vector3 tip = hW + dW * (hFace.cellSize * 0.35f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DestroyImmediate(head.GetComponent<Collider>());
        head.transform.SetParent(root);
        head.transform.position = tip;
        head.transform.rotation = Quaternion.LookRotation(dW, hN) * Quaternion.Euler(0, 0, 45f);
        float ts = lineWidth * 2f;
        head.transform.localScale = new Vector3(ts, lineDepth, ts);
        head.GetComponent<MeshRenderer>().material = inkMat;
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
