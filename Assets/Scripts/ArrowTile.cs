using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class ArrowTile : MonoBehaviour
{
    [Header("Tile Settings")]
    public FaceGrid faceGrid;
    public ArrowSpawnData data;
    public int startFaceIndex;
    public Vector2Int GridCoords;

    [Header("Visual Segments")]
    public List<Transform> segments = new List<Transform>();
    public List<GameObject> outlineObjects = new List<GameObject>();

    // Runtime
    private List<BodyCell> allCells;
    private bool isSliding = false;

    public void Setup(FaceGrid grid, ArrowSpawnData spawnData, List<BodyCell> cells, int faceIndex)
    {
        faceGrid = grid;
        data = spawnData;
        GridCoords = spawnData.position;
        startFaceIndex = faceIndex;
        allCells = cells;

        transform.localPosition = grid.GetLocalPosition(GridCoords);
        RegisterAllCells();
    }

    void Start()
    {
        // Recompute từ data đã serialize (private fields bị null khi vào Play mode)
        if (data != null)
        {
            allCells = data.GetAllCells(startFaceIndex);
        }
        RegisterAllCells();
    }

    private void RegisterAllCells()
    {
        if (allCells == null) return;
        foreach (var c in allCells)
        {
            FaceGrid face = FaceGrid.GetFace(c.faceIndex);
            if (face != null && !face.IsOutOfBounds(c.position))
                face.RegisterArrowCells(this, new List<Vector2Int> { c.position });
        }
    }

    private void UnregisterAllCells()
    {
        if (allCells == null) return;
        foreach (var c in allCells)
        {
            FaceGrid face = FaceGrid.GetFace(c.faceIndex);
            if (face != null)
                face.UnregisterArrowCells(this, new List<Vector2Int> { c.position });
        }
    }

    // ================================================================
    // SLIDE — Entry point khi người chơi tap
    // ================================================================
    public void Slide()
    {
        if (isSliding) return;
        if (data == null) return;

        Vector2Int dir = data.initialDirection;
        if (dir == Vector2Int.zero) return;

        if (!CanHeadEscape())
        {
            PlayBumpBounce();
            return;
        }

        PerformSnakeEscape();
    }

    // ================================================================
    // COLLISION: Dò đường từ HEAD xuyên nhiều mặt qua CubeTopology
    // ================================================================
    /// <summary>
    /// Quét đường đi phía trước ĐẦU mũi tên — CHỈ trên mặt hiện tại.
    /// Khi Head chạm mép → thoát ra không gian 3D → return true.
    /// Chỉ cần đầu thoát được ra khỏi mép mặt → toàn bộ thân sẽ theo.
    /// </summary>
    public bool CanHeadEscape()
    {
        Vector2Int currPos = data.position;        // Vị trí Head
        Vector2Int currDir = data.initialDirection;

        FaceGrid headGrid = FaceGrid.GetFace(startFaceIndex);
        if (headGrid == null) return true;

        // Quét từ Head → mép mặt hiện tại
        Vector2Int checkPos = currPos + currDir;
        while (!headGrid.IsOutOfBounds(checkPos))
        {
            ArrowTile other = headGrid.GetArrowAt(checkPos);
            if (other != null && other != this)
                return false; // Bị chặn bởi mũi tên KHÁC trên cùng mặt

            checkPos += currDir;
        }

        // Head đã chạm mép mặt → bay ra không gian 3D → thoát!
        return true;
    }

    // ================================================================
    // ANIMATION: Rắn trườn — từng đốt trượt theo đốt phía trước
    // ================================================================
    private void PerformSnakeEscape()
    {
        isSliding = true;
        UnregisterAllCells();
        GameManager.Instance?.OnBlockEscaped();

        // === Xây danh sách World Position cho từng cell ===
        // allCells[0] = Head, allCells[1] = Body1, ..., allCells[N-1] = Tail
        List<Vector3> cellWorldPositions = new List<Vector3>();
        foreach (var cell in allCells)
        {
            FaceGrid face = FaceGrid.GetFace(cell.faceIndex);
            if (face != null)
                cellWorldPositions.Add(face.GetWorldPosition(cell.position));
            else
                cellWorldPositions.Add(transform.position);
        }

        // Hướng bay 3D thế giới của Head
        float flyDirX = data.initialDirection.x;
        float flyDirY = data.initialDirection.y;
        if (startFaceIndex < 4) flyDirX = -flyDirX;
        else if (startFaceIndex == 5) flyDirY = -flyDirY;
        Vector3 headFlyDir = faceGrid.transform.TransformDirection(
            new Vector3(flyDirX, flyDirY, 0f)).normalized;

        // Điểm bay xa (Head bay ra khỏi cube)
        Vector3 headStart = cellWorldPositions[0];
        float flyDist = faceGrid.gridWidth * faceGrid.cellSize * 3f;
        Vector3 headFlyTarget = headStart + headFlyDir * flyDist;

        // === Animate từng Segment ===
        float staggerDelay = 0.06f;  // Độ trễ giữa mỗi đốt
        float slideSpeed = 0.08f;    // Thời gian mỗi đốt trượt qua 1 cell
        int segCount = Mathf.Min(segments.Count, allCells.Count);

        Sequence masterSeq = DOTween.Sequence();

        for (int i = 0; i < segCount; i++)
        {
            Transform seg = segments[i];
            if (seg == null) continue;

            // Tách segment khỏi root để animate độc lập (world space)
            seg.SetParent(null);

            float startTime = i * staggerDelay;

            if (i == 0)
            {
                // HEAD: bay thẳng ra ngoài theo hướng mũi tên
                masterSeq.Insert(startTime,
                    seg.DOMove(headFlyTarget, 0.5f).SetEase(Ease.InCubic));
            }
            else
            {
                // BODY/TAIL: trượt qua vị trí các đốt phía trước → rồi bay ra
                List<Vector3> path = new List<Vector3>();

                // Trượt ngược từ vị trí đốt i-1 về đốt 0 (Head)
                for (int j = i - 1; j >= 0; j--)
                    path.Add(cellWorldPositions[j]);

                // Cuối cùng bay ra ngoài theo Head
                path.Add(headFlyTarget);

                float totalDur = path.Count * slideSpeed;

                masterSeq.Insert(startTime,
                    seg.DOPath(path.ToArray(), totalDur, PathType.Linear)
                       .SetEase(Ease.Linear));
            }
        }

        // Sau khi tất cả segments bay xong → Destroy root
        masterSeq.OnComplete(() =>
        {
            // Destroy từng segment đã tách ra
            foreach (var seg in segments)
            {
                if (seg != null) Destroy(seg.gameObject);
            }
            Destroy(gameObject);
        });
    }

    // ================================================================
    // BUMP BOUNCE — Rung lắc khi bị chặn
    // ================================================================
    private void PlayBumpBounce()
    {
        isSliding = true;

        // Hiện viền đỏ
        foreach (var outline in outlineObjects)
        {
            if (outline != null) outline.SetActive(true);
        }

        float flyDirX = data.initialDirection.x;
        float flyDirY = data.initialDirection.y;
        if (startFaceIndex < 4) flyDirX = -flyDirX;
        else if (startFaceIndex == 5) flyDirY = -flyDirY;
        Vector3 worldDir = faceGrid.transform.TransformDirection(
            new Vector3(flyDirX, flyDirY, 0f)).normalized;

        Vector3 rootOrig = transform.position;
        Vector3 rootBump = rootOrig + worldDir * 0.15f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(rootBump, 0.06f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOMove(rootOrig, 0.06f).SetEase(Ease.InQuad));
        seq.AppendInterval(0.25f); // Giữ viền đỏ thêm 0.25s
        seq.OnComplete(() =>
        {
            // Ẩn viền đỏ
            foreach (var outline in outlineObjects)
            {
                if (outline != null) outline.SetActive(false);
            }
            isSliding = false;
        });
    }
}
