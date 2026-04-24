using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // UI 컴포넌트 사용을 위해 필수

public class MainSceneVolumn : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Audio")]
    public AudioSource bgmSource;
    public Slider volumeSlider;

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);


        if (volumeSlider != null && bgmSource != null)
        {
            float saved = PlayerPrefs.GetFloat("BGMVolume", 1f);
            volumeSlider.value = saved;
            bgmSource.volume = saved;

            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    public void ToggleSettings()
    {
        bool isActive = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isActive);

        if (isActive)
        {
            // 설정창이 열릴 때
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None; // 커서 해제
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetVolume(float value)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = value;
            PlayerPrefs.SetFloat("BGMVolume", value);
        }
    }
}