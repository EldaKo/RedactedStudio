using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button StartBtn;
    [SerializeField] private Button QuitBtn;
    [SerializeField] private Button TutorialBtn;

    void Start()
    {
 
        if (StartBtn != null)
        {
            StartBtn.onClick.AddListener(PlayGame);
        }

        if (QuitBtn != null)
        {
            QuitBtn.onClick.AddListener(QuitGame);
        }

        if (TutorialBtn != null)
        {
            TutorialBtn.onClick.AddListener(StartTutorial);
        }

    }
    public void PlayGame()
    {
        Debug.Log("start clicked!");
        SceneManager.LoadScene("CityMapScene");
    }
    public void QuitGame()
    {
        Debug.Log("quit clicked!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 실제 빌드된 게임에서 종료
            Application.Quit();
#endif
    }

    public void StartTutorial()
    {
        Debug.Log("tutorial clicked!");
        SceneManager.LoadScene("TutorialScene");
    }
}