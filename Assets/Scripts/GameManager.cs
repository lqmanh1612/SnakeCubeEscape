using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Configuration")]
    public ArrowLevelData currentLevelData;

    [Header("Runtime State")]
    public int totalBlocks;

    [Header("UI UI Elements")]
    public TextMeshProUGUI countText;

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

        if (countText == null)
        {
            GameObject textObj = GameObject.Find("CountText");
            if (textObj != null)
            {
                countText = textObj.GetComponent<TextMeshProUGUI>();
            }
        }

        UpdateCountUI();
    }

    public void OnBlockEscaped()
    {
        totalBlocks--;
        UpdateCountUI();

        if (totalBlocks <= 0)
        {
            LevelComplete();
        }
    }

    private void UpdateCountUI()
    {
        if (countText != null)
        {
            countText.text = totalBlocks.ToString();
        }
    }

    private void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE! All arrows cleared.");
    }
}
