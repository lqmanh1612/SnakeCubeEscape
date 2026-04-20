using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI textLevel;
    public Button buttonPlay;
    public Button buttonReset;

    private void Start()
    {
        // Tự động tìm TextMeshPro nếu chưa gán
        if (textLevel == null)
        {
            GameObject levelObj = GameObject.Find("TextLevel");
            if (levelObj != null)
            {
                textLevel = levelObj.GetComponent<TextMeshProUGUI>();
            }
        }

        // Tự động tìm và gán chức năng cho các Button nếu chưa gán
        if (buttonPlay == null)
        {
            GameObject obj = GameObject.Find("ButtonPlay");
            if (obj != null) buttonPlay = obj.GetComponent<Button>();
        }

        if (buttonReset == null)
        {
            GameObject obj = GameObject.Find("ButtonReset");
            if (obj != null) buttonReset = obj.GetComponent<Button>();
        }

        // Đăng ký sự kiện
        if (buttonPlay != null) buttonPlay.onClick.AddListener(LoadGameplayScene);
        if (buttonReset != null) buttonReset.onClick.AddListener(ResetProgress);

        UpdateLevelDisplay();
    }

    private void LoadGameplayScene()
    {
        SceneManager.LoadScene("GameplayScene");
    }

    private void ResetProgress()
    {
        // Xóa tiến trình về Level 1
        PlayerPrefs.SetInt("CurrentLevelIndex", 0);
        PlayerPrefs.Save();
        
        Debug.Log("Progress Reset to Level 1");
        UpdateLevelDisplay();
    }

    private void UpdateLevelDisplay()
    {
        if (textLevel != null)
        {
            // Lấy level hiện tại từ PlayerPrefs (mặc định là 0 nếu chưa chơi)
            int currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex", 0);
            
            // Hiển thị Level (Index + 1)
            textLevel.text = "Level " + (currentLevelIndex + 1);
        }
    }
}
