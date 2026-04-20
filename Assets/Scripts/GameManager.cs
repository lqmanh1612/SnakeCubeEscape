using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Configuration")]
    public LevelDatabase database;
    public ArrowLevelData currentLevelData;

    [Header("Progression")]
    public TextMeshProUGUI textLevel;
    private int currentLevelIndex;

    [Header("Runtime State")]
    public int totalBlocks;

    [Header("UI Elements")]
    public TextMeshProUGUI countText;

    [Header("Audio Settings")]
    public AudioClip bgmClip;
    public AudioClip escapeClip;
    public AudioClip bumpClip;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Khởi tạo tự động các AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        InitializeLevel();
        
        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }

        if (countText == null)
        {
            GameObject textObj = GameObject.Find("CountText");
            if (textObj != null) countText = textObj.GetComponent<TextMeshProUGUI>();
        }

        if (textLevel == null)
        {
            GameObject levelObj = GameObject.Find("TextLevel");
            if (levelObj != null) textLevel = levelObj.GetComponent<TextMeshProUGUI>();
        }

        UpdateUI();
    }

    private void InitializeLevel()
    {
        // 1. Lấy chỉ số màn chơi hiện tại
        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);

        if (database != null && database.levels.Count > 0)
        {
            // Kiểm tra giới hạn màn chơi (quần lại màn đầu nếu hết level, hoặc có thể custom)
            if (currentLevelIndex >= database.levels.Count)
            {
                currentLevelIndex = 0; // Hoặc hiện màn Victory
                PlayerPrefs.SetInt("CurrentLevelIndex", 0);
            }

            currentLevelData = database.levels[currentLevelIndex];
        }

        // 2. Sinh level động
        if (currentLevelData != null)
        {
            LevelGenerator.Instance.Generate(currentLevelData, database);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy dữ liệu level để khởi tạo!");
        }

        // 3. Đếm số mũi tên sau khi sinh
        ArrowTile[] arrows = FindObjectsOfType<ArrowTile>();
        totalBlocks = arrows.Length;
    }

    public void PlayEscapeSound()
    {
        if (sfxSource != null && escapeClip != null)
        {
            sfxSource.PlayOneShot(escapeClip);
        }
    }

    public void PlayBumpSound()
    {
        if (sfxSource != null && bumpClip != null)
        {
            sfxSource.PlayOneShot(bumpClip);
        }
    }

    public void OnBlockEscaped()
    {
        totalBlocks--;
        UpdateUI();

        if (totalBlocks <= 0)
        {
            LevelComplete();
        }
    }

    private void UpdateUI()
    {
        if (countText != null)
        {
            countText.text = totalBlocks.ToString();
        }

        if (textLevel != null)
        {
            textLevel.text = "Level " + (currentLevelIndex + 1);
        }
    }

    private void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE! All arrows cleared.");
        
        // Tăng level và lưu lại
        currentLevelIndex++;
        PlayerPrefs.SetInt("CurrentLevelIndex", currentLevelIndex);
        PlayerPrefs.Save();

        // Chuyển sang màn tiếp theo (reload lại scene để InitializeLevel chạy lại)
        StartCoroutine(LoadNextLevelWithDelay());
    }

    private IEnumerator LoadNextLevelWithDelay()
    {
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
