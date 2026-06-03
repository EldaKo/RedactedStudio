using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button continueBtn;
    [SerializeField] private Button newGameBtn;
    [SerializeField] private Button tutorialBtn;
    [SerializeField] private Button quitBtn;

    void Start()
    {
        if (continueBtn != null)
        {
            continueBtn.onClick.AddListener(ContinueGame);
            continueBtn.interactable = SaveManager.HasSave();
        }

        if (newGameBtn != null) newGameBtn.onClick.AddListener(NewGame);
        if (tutorialBtn != null) tutorialBtn.onClick.AddListener(StartTutorial);
        if (quitBtn != null) quitBtn.onClick.AddListener(QuitGame);
    }

    public void ContinueGame()
    {
        Debug.Log("continue clicked!");
        SaveManager.ContinueGame();
    }

    public void NewGame()
    {
        Debug.Log("new game clicked!");
        SaveManager.StartNewGame();
    }

    public void StartTutorial()
    {
        Debug.Log("tutorial clicked!");
        SceneManager.LoadScene("TutorialScene");
    }

    public void QuitGame()
    {
        Debug.Log("quit clicked!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
