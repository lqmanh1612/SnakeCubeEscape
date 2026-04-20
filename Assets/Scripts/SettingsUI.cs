using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelSetting;
    public Button buttonSetting;
    public Button buttonHome;
    public Button buttonRestart;
    public Button buttonBackGame;
    public Scrollbar scrollbarSound;

    private const string SoundPrefKey = "GameVolume";

    private void Start()
    {
        // Tự động tìm kiếm nếu chưa được gán trong Inspector (hỗ trợ cả các object đang bị disable)
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (panelSetting == null && child.name == "PanelSetting") panelSetting = child.gameObject;
            if (buttonSetting == null && child.name == "ButtonSetting") buttonSetting = child.GetComponent<Button>();
            if (buttonHome == null && child.name == "ButtonHome") buttonHome = child.GetComponent<Button>();
            if (buttonRestart == null && child.name == "ButtonRestart") buttonRestart = child.GetComponent<Button>();
            if (buttonBackGame == null && child.name == "ButtonBackGame") buttonBackGame = child.GetComponent<Button>();
            if (scrollbarSound == null && child.name == "ScrollbarSound") scrollbarSound = child.GetComponent<Scrollbar>();
        }

        // Khởi tạo các sự kiện nút
        if (buttonSetting != null) buttonSetting.onClick.AddListener(OpenSettings);
        if (buttonHome != null) buttonHome.onClick.AddListener(GotoHome);
        if (buttonRestart != null) buttonRestart.onClick.AddListener(RestartGame);
        if (buttonBackGame != null) buttonBackGame.onClick.AddListener(CloseSettings);
        
        // Khởi tạo âm lượng
        if (scrollbarSound != null)
        {
            scrollbarSound.onValueChanged.AddListener(OnSoundVolumeChanged);
            // Setup âm lượng mặc định từ trước khi vào game (Default 1.0)
            float savedVolume = PlayerPrefs.GetFloat(SoundPrefKey, 1.0f);
            scrollbarSound.value = savedVolume;
            AudioListener.volume = savedVolume;
        }

        // Đảm bảo lúc bắt đầu game thì tắt Panel đi
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);
        }
    }

    private void OpenSettings()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);
        }
        // Tạm dừng thời gian game
        Time.timeScale = 0;
    }

    private void CloseSettings()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);
        }
        // Tiếp tục thời gian game
        Time.timeScale = 1;
    }

    private void GotoHome()
    {
        // Luôn reset timeScale về 1 trước khi load scene để tránh bị treo game ở scene mới
        Time.timeScale = 1;
        SceneManager.LoadScene("HomeScene");
    }

    private void RestartGame()
    {
        // Reset timeScale trước khi reload màn chơi
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnSoundVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(SoundPrefKey, value);
        PlayerPrefs.Save();
    }
}
