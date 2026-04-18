using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Configuration")]
    public ArrowLevelData currentLevelData;

    [Header("Runtime State")]
    public int totalBlocks;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Đếm lại bằng tay khi khởi chạy
        ArrowTile[] arrows = FindObjectsOfType<ArrowTile>();
        totalBlocks = arrows.Length;
    }

    public void OnBlockEscaped()
    {
        totalBlocks--;
        if (totalBlocks <= 0)
        {
            LevelComplete();
        }
    }

    private void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE! All arrows cleared.");
    }
}
