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
            // 설정창이 닫힐 때
            Time.timeScale = 1f;

            // [중요] 메인 화면이라면 커서를 가두지 말고 다시 풀어줘야 합니다!
            // 만약 게임 플레이 중이 아니라 '메인 메뉴'라면 아래 두 줄을 수정하세요.
            Cursor.lockState = CursorLockMode.None; // Locked 대신 None으로 변경
            Cursor.visible = true;                 // false 대신 true로 변경
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