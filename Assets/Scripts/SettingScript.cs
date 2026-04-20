using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;
    
    [SerializeField] private Button goMainBtn;
    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (goMainBtn != null)
        {
            goMainBtn.onClick.AddListener(quitGame);
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
    public void quitGame()
    {
        //Debug.Log("start clicked!");
        SceneManager.LoadScene("mainScreen");
    }
}