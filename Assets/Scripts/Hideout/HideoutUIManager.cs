using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HideoutUIManager : MonoBehaviour
{
    public static HideoutUIManager Instance;

    [Header("공통 버튼 (모든 시설 공유)")]
    public Button exitToMainButton;
    [Tooltip("게임을 저장하고 메인 화면으로 돌아감")]
    public Button saveAndExitButton;
    [Tooltip("레이드(시티) 씬으로 입장")]
    public Button enterRaidButton;

    [Header("씬 이름")]
    public string raidSceneName = "DEMO City Ruins";
    public string mainScreenSceneName = "mainScreen";

    void Awake()
    {
        Instance = this;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (exitToMainButton != null) exitToMainButton.onClick.AddListener(GoToMainScreen);
        if (saveAndExitButton != null) saveAndExitButton.onClick.AddListener(SaveAndExit);
        if (enterRaidButton != null) enterRaidButton.onClick.AddListener(EnterRaid);
    }

    void Start()
    {
        ApplyOrSaveOnEnter();
    }

    private void ApplyOrSaveOnEnter()
    {
        if (SaveManager.PendingLoad != null)
        {
            ApplySaveData(SaveManager.PendingLoad);
            SaveManager.PendingLoad = null;
        }
        else if (SaveManager.SaveOnHideoutLoad)
        {
            SaveManager.Save(SaveManager.CaptureCurrentState());
        }
        SaveManager.SaveOnHideoutLoad = false;
    }

    private void ApplySaveData(GameSaveData data)
    {
        if (PlayerUpgradeManager.Instance != null)
        {
            PlayerUpgradeManager.Instance.armorLevel = data.armorLevel;
            PlayerUpgradeManager.Instance.weaponLevelList.Clear();
            foreach (var w in data.weaponLevels)
                PlayerUpgradeManager.Instance.weaponLevelList.Add(new WeaponLevelData { weaponName = w.weaponName, level = w.level });
        }

        FacilityLevelTracker.Clear();
        foreach (var f in data.facilities) FacilityLevelTracker.SetLevel(f.facilityName, f.currentLevel);
        foreach (var f in FindObjectsOfType<FacilityScript>(true))
            f.currentLevel = FacilityLevelTracker.GetLevel(f.facilityName, f.currentLevel);

        if (Inventory.HasInstance)
        {
            Inventory.Instance.ClearAll();
            var registry = ItemDataRegistry.Get();
            if (registry == null)
            {
                Debug.LogWarning("[Save] ItemDataRegistry not found at Resources/ItemDataRegistry");
            }
            else
            {
                foreach (var slot in data.inventorySlots)
                {
                    var item = registry.FindByName(slot.itemDataName);
                    if (item != null) Inventory.Instance.SetSlot(slot.slotIndex, item, slot.count);
                    else Debug.LogWarning($"[Save] ItemData '{slot.itemDataName}' not in registry");
                }
            }
        }

        Debug.Log("[Save] Applied save data");
    }

    public void SaveAndExit()
    {
        SaveManager.Save(SaveManager.CaptureCurrentState());
        SceneManager.LoadScene(mainScreenSceneName);
    }

    public void EnterRaid()
    {
        SaveManager.Save(SaveManager.CaptureCurrentState());
        EscapeItemRegistry.Reset();
        SceneManager.LoadScene(raidSceneName);
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
        SceneManager.LoadScene(mainScreenSceneName);
    }
}
