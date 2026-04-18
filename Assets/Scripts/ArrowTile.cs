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

    // Runtime
    private List<BodyCell> allCells;
    private List<Vector2Int> homeFaceCells; // Chỉ ô trên mặt gốc
    private bool isSliding = false;

    public void Setup(FaceGrid grid, ArrowSpawnData spawnData, List<BodyCell> cells, int faceIndex)
    {
        faceGrid = grid;
        data = spawnData;
        GridCoords = spawnData.position;
        startFaceIndex = faceIndex;
        allCells = cells;

        // Tách ô trên mặt gốc (dùng cho collision + slide)
        homeFaceCells = new List<Vector2Int>();
        foreach (var c in allCells)
            if (c.faceIndex == faceIndex) homeFaceCells.Add(c.position);

        transform.localPosition = grid.GetLocalPosition(GridCoords);
        RegisterAllCells();
    }

    void Start()
    {
        // Recompute từ data đã serialize (private fields bị null khi vào Play mode)
        if (data != null)
        {
            allCells = data.GetAllCells(startFaceIndex);
            homeFaceCells = new List<Vector2Int>();
            foreach (var c in allCells)
                if (c.faceIndex == startFaceIndex) homeFaceCells.Add(c.position);
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

    public void Slide()
    {
        if (isSliding) return;
        if (homeFaceCells == null || homeFaceCells.Count == 0) return;

        Vector2Int dir = data.initialDirection;
        if (dir == Vector2Int.zero) return;

        // ======== COLLISION: quét TOÀN BỘ đường đi từ mỗi ô tới mép ========
        foreach (var cell in homeFaceCells)
        {
            Vector2Int checkPos = cell + dir;
            while (!faceGrid.IsOutOfBounds(checkPos))
            {
                ArrowTile other = faceGrid.GetArrowAt(checkPos);
                if (other != null && other != this)
                {
                    PlayBumpBounce();
                    return;
                }
                checkPos += dir;
            }
        }

        // ======== SLIDE ========
        isSliding = true;
        UnregisterAllCells();
        GameManager.Instance?.OnBlockEscaped();

        // Tính số bước để toàn bộ thân trên mặt gốc thoát lưới
        int maxSteps = 0;
        foreach (var cell in homeFaceCells)
        {
            int steps;
            if (dir.x > 0)      steps = faceGrid.gridWidth - cell.x;
            else if (dir.x < 0) steps = cell.x + 1;
            else if (dir.y > 0) steps = faceGrid.gridHeight - cell.y;
            else                steps = cell.y + 1;
            maxSteps = Mathf.Max(maxSteps, steps);
        }
        if (maxSteps == 0) maxSteps = 5;

        Vector3 stepVec = new Vector3(dir.x, dir.y, 0f) * faceGrid.cellSize;
        Vector3 startPos = transform.localPosition;
        Vector3 edgePos = startPos + stepVec * maxSteps;
        Vector3 flyPos = edgePos + stepVec * 5f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMove(edgePos, maxSteps * 0.07f).SetEase(Ease.Linear));
        seq.Append(transform.DOLocalMove(flyPos, 0.25f).SetEase(Ease.InCubic));
        seq.OnComplete(() => Destroy(gameObject));
    }

    private void PlayBumpBounce()
    {
        isSliding = true;
        Vector3 orig = transform.localPosition;
        Vector3 bump = orig + new Vector3(data.initialDirection.x, data.initialDirection.y, 0f) * 0.2f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMove(bump, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOLocalMove(orig, 0.08f).SetEase(Ease.InQuad));
        seq.OnComplete(() => isSliding = false);
    }
}
