using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HideoutUIManager : MonoBehaviour
{
    public static HideoutUIManager Instance;

    [Header("공통 버튼 (모든 시설 공유)")]
    public Button exitToMainButton;

    void Awake()
    {
        Instance = this;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (exitToMainButton != null) exitToMainButton.onClick.AddListener(GoToMainScreen);
    }

    public void OpenFacility(FacilityScript facility)
    {
        foreach (var f in FindObjectsOfType<FacilityScript>(true))
        {
            if (f != facility) f.Close();
        }
        facility.Open();
    }

    public void CloseAll()
    {
        foreach (var f in FindObjectsOfType<FacilityScript>(true))
            f.Close();

        if (HideoutCamera.Instance != null) HideoutCamera.Instance.ReturnToTopView();
    }

    public void GoToMainScreen()
    {
        SceneManager.LoadScene("mainScreen");
    }
}
