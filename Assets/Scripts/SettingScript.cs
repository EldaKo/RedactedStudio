using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NeoFPS;

public class SettingScript : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;

    [SerializeField] private Button goMainBtn;

    [Header("Audio")]
    public AudioSource bgmSource;
    public Slider volumeSlider;

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (goMainBtn != null)
            goMainBtn.onClick.AddListener(quitGame);

        if (volumeSlider != null && bgmSource != null)
        {
            float saved = PlayerPrefs.GetFloat("BGMVolume", 1f);
            volumeSlider.value = saved;
            bgmSource.volume = saved;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (FpsSettings.audio != null)
        {
            FpsSettings.audio.onMusicVolumeChanged += ApplyMusicVolume;
            FpsSettings.audio.onMasterVolumeChanged += ApplyMasterVolume;
            ApplyMusicVolume(FpsSettings.audio.musicVolume);
        }
    }

    void OnDestroy()
    {
        if (FpsSettings.audio != null)
        {
            FpsSettings.audio.onMusicVolumeChanged -= ApplyMusicVolume;
            FpsSettings.audio.onMasterVolumeChanged -= ApplyMasterVolume;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleSettings();
    }

    private void ApplyMusicVolume(float value)
    {
        if (bgmSource != null)
            bgmSource.volume = value * (FpsSettings.audio != null ? FpsSettings.audio.masterVolume : 1f);
    }

    private void ApplyMasterVolume(float value)
    {
        if (bgmSource != null)
            bgmSource.volume = (FpsSettings.audio != null ? FpsSettings.audio.musicVolume : 1f) * value;
    }

    public void ToggleSettings()
    {
        bool isActive = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isActive);

        if (isActive)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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

    public void quitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("mainScreen");
    }
}