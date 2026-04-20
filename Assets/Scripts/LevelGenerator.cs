using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    private static LevelGenerator _instance;
    public static LevelGenerator Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("LevelGenerator");
                _instance = go.AddComponent<LevelGenerator>();
            }
            return _instance;
        }
    }

    public void Generate(ArrowLevelData levelData, LevelDatabase database)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData is null! Cannot generate level.");
            return;
        }

        // 1. Clear existing level
        ClearLevel();

        int gridSize = levelData.gridSize;
        StructurePivot pivot = FindObjectOfType<StructurePivot>();
        if (pivot == null)
        {
            pivot = new GameObject("StructurePivot").AddComponent<StructurePivot>();
        }
        pivot.transform.position = Vector3.zero;
        pivot.transform.rotation = Quaternion.identity;

        // 2. Core Cube
        float coreSize = gridSize * 1f;
        GameObject coreCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coreCube.name = "CentralCube_Visual";
        coreCube.transform.localScale = Vector3.one * coreSize * 0.98f;
        coreCube.transform.SetParent(pivot.transform);
        coreCube.transform.localPosition = Vector3.zero;
        
        Collider col = coreCube.GetComponent<Collider>();
        if (col != null) Destroy(col);

        if (database != null && database.centralCubeMaterial != null)
        {
            coreCube.GetComponent<MeshRenderer>().sharedMaterial = database.centralCubeMaterial;
        }

        // 3. Setup Camera (Optional, depends on design)
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(coreSize + 2, coreSize + 2, -coreSize - 2);
            Camera.main.transform.LookAt(Vector3.zero);
        }

        // 4. Generate faces
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

        FaceGrid[] grids = new FaceGrid[6];
        for (int i = 0; i < 6; i++)
        {
            GameObject faceObj = new GameObject($"Face_{i}");
            faceObj.transform.SetParent(pivot.transform);
            faceObj.transform.localPosition = fPos[i];
            faceObj.transform.localEulerAngles = fRot[i];
            FaceGrid grid = faceObj.AddComponent<FaceGrid>();
            grid.SetFaceIndex(i); // Đăng ký index chính xác
            grid.gridWidth = gridSize;
            grid.gridHeight = gridSize;
            grid.cellSize = 1f;
            grids[i] = grid;
        }

        // 5. Build Arrows
        Material inkMat = (database != null) ? database.inkMaterial : null;
        Material headMat = (database != null) ? database.headMaterial : null;
        Material outlineMat = (database != null) ? database.outlineMaterial : null;

        for (int f = 0; f < 6; f++)
        {
            if (f < levelData.faces.Length && levelData.faces[f].arrows != null)
            {
                foreach (var arrData in levelData.faces[f].arrows)
                {
                    BuildArrow(grids, f, arrData, inkMat, headMat, outlineMat, pivot.transform);
                }
            }
        }

        Debug.Log($"Level generated: {levelData.name} (Grid Size: {gridSize})");
    }

    private void ClearLevel()
    {
        StructurePivot pivot = FindObjectOfType<StructurePivot>();
        if (pivot != null)
        {
            // Destroy all children
            for (int i = pivot.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(pivot.transform.GetChild(i).gameObject);
            }
        }

        // Also clean up stray FaceGrids if any outside pivot
        FaceGrid[] grids = FindObjectsOfType<FaceGrid>();
        foreach (var g in grids)
        {
            if (g.transform.parent != (pivot ? pivot.transform : null))
                Destroy(g.gameObject);
        }
    }

    private void BuildArrow(FaceGrid[] grids, int faceIndex, ArrowSpawnData data, Material inkMat, Material headMat, Material outlineMat, Transform parent)
    {
        FaceGrid homeGrid = grids[faceIndex];
        List<BodyCell> allCells = data.GetAllCells(faceIndex);

        GameObject arrowRoot = new GameObject($"Arrow_F{faceIndex}_{data.position.x}_{data.position.y}");
        arrowRoot.transform.SetParent(homeGrid.transform);
        arrowRoot.transform.localPosition = homeGrid.GetLocalPosition(data.position);

        ArrowTile tile = arrowRoot.AddComponent<ArrowTile>();
        tile.Setup(homeGrid, data, allCells, faceIndex);

        List<Transform> segments = new List<Transform>();
        List<GameObject> outlines = new List<GameObject>();
        
        DrawFlatInkSegmented(arrowRoot, grids, allCells, data.initialDirection,
                             faceIndex, inkMat, headMat, outlineMat, segments, outlines);
        
        tile.segments = segments;
        tile.outlineObjects = outlines;

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

    private void DrawFlatInkSegmented(GameObject arrowRoot, FaceGrid[] grids,
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
        float outlineScale = 1.25f;

        for (int i = 0; i < allCells.Count; i++)
        {
            BodyCell cell = allCells[i];
            FaceGrid face = grids[cell.faceIndex];
            Vector3 fN = face.transform.forward;
            Vector3 wPos = face.GetWorldPosition(cell.position) + fN * surfOff;

            GameObject seg = new GameObject($"Segment_{i}");
            seg.transform.position = wPos;
            seg.transform.rotation = face.transform.rotation;
            seg.transform.SetParent(root, true);

            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (vis.GetComponent<Collider>()) Destroy(vis.GetComponent<Collider>());
            vis.transform.SetParent(seg.transform);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.identity;
            
            float bodySize = (i == 0) ? (cellSize * 0.65f) : (cellSize * 0.4f);
            vis.transform.localScale = new Vector3(bodySize, bodySize, lineDepth);
            vis.GetComponent<MeshRenderer>().sharedMaterial = (i == 0) ? headMat : inkMat;

            GameObject outline = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (outline.GetComponent<Collider>()) Destroy(outline.GetComponent<Collider>());
            outline.name = "Outline";
            outline.transform.SetParent(seg.transform);
            outline.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = new Vector3(bodySize * outlineScale, bodySize * outlineScale, lineDepth * 0.5f);
            outline.GetComponent<MeshRenderer>().sharedMaterial = outlineMat;
            outline.SetActive(false);
            outOutlines.Add(outline);

            if (i == 0)
            {
                float flyDirX = flightDir.x;
                float flyDirY = flightDir.y;
                if (cell.faceIndex < 4) flyDirX = -flyDirX;
                else if (cell.faceIndex == 5) flyDirY = -flyDirY;
                Vector3 dW = face.transform.TransformDirection(new Vector3(flyDirX, flyDirY, 0f)).normalized;
                Vector3 tip = wPos + dW * (cellSize * 0.35f);

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (head.GetComponent<Collider>()) Destroy(head.GetComponent<Collider>());
                head.transform.SetParent(seg.transform);
                head.transform.position = tip;
                head.transform.rotation = Quaternion.LookRotation(dW, fN) * Quaternion.Euler(0, 0, 45f);
                float ts = lineWidth * 2.2f;
                head.transform.localScale = new Vector3(ts, lineDepth, ts);
                head.GetComponent<MeshRenderer>().sharedMaterial = headMat;
            }

            if (i < allCells.Count - 1)
            {
                BodyCell nextCell = allCells[i + 1];
                if (nextCell.faceIndex == cell.faceIndex)
                {
                    Vector3 wNext = face.GetWorldPosition(nextCell.position) + fN * surfOff;
                    float dist = Vector3.Distance(wPos, wNext);
                    if (dist > 0.01f)
                    {
                        Vector3 center = (wPos + wNext) / 2f;
                        Vector3 dir = (wNext - wPos).normalized;

                        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        if (line.GetComponent<Collider>()) Destroy(line.GetComponent<Collider>());
                        line.transform.SetParent(seg.transform);
                        line.transform.position = center;
                        line.transform.rotation = Quaternion.LookRotation(dir, fN);
                        line.transform.localScale = new Vector3(lineWidth, lineDepth, dist + lineWidth * 0.4f);
                        line.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
                    }
                }
                else
                {
                    FaceGrid nextFace = grids[nextCell.faceIndex];
                    Vector3 nextFN = nextFace.transform.forward;
                    Vector3 wNext = nextFace.GetWorldPosition(nextCell.position) + nextFN * surfOff;

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

                    float dist1 = Vector3.Distance(wPos, edgeMid);
                    if (dist1 > 0.01f)
                    {
                        Vector3 center1 = (wPos + edgeMid) / 2f;
                        Vector3 dir1 = (edgeMid - wPos).normalized;

                        GameObject line1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        if (line1.GetComponent<Collider>()) Destroy(line1.GetComponent<Collider>());
                        line1.transform.SetParent(seg.transform);
                        line1.transform.position = center1;
                        line1.transform.rotation = Quaternion.LookRotation(dir1, fN);
                        line1.transform.localScale = new Vector3(lineWidth, lineDepth, dist1 + lineWidth * 0.2f);
                        line1.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
                    }

                    float dist2 = Vector3.Distance(edgeMid, wNext);
                    if (dist2 > 0.01f)
                    {
                        Vector3 center2 = (edgeMid + wNext) / 2f;
                        Vector3 dir2 = (wNext - edgeMid).normalized;

                        GameObject line2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        if (line2.GetComponent<Collider>()) Destroy(line2.GetComponent<Collider>());
                        line2.transform.SetParent(seg.transform);
                        line2.transform.position = center2;
                        line2.transform.rotation = Quaternion.LookRotation(dir2, nextFN);
                        line2.transform.localScale = new Vector3(lineWidth, lineDepth, dist2 + lineWidth * 0.2f);
                        line2.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
                    }
                }
            }
            outSegments.Add(seg.transform);
        }
    }
}
