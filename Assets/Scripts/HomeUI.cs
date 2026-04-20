using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI textLevel;
    public Button buttonPlay;

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

        // Tự động tìm và gán chức năng cho ButtonPlay nếu chưa gán
        if (buttonPlay == null)
        {
            GameObject playBtnObj = GameObject.Find("ButtonPlay");
            if (playBtnObj != null)
            {
                buttonPlay = playBtnObj.GetComponent<Button>();
            }
        }

        if (buttonPlay != null)
        {
            buttonPlay.onClick.AddListener(LoadGameplayScene);
        }

        UpdateLevelDisplay();
    }

    private void LoadGameplayScene()
    {
        SceneManager.LoadScene("GameplayScene");
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
