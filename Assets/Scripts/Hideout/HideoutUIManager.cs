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
            // Continue / Death: 저장된 상태 복원, 현재 인벤토리는 폐기
            ApplySaveData(SaveManager.PendingLoad);
            SaveManager.PendingLoad = null;
        }
        else if (SaveManager.SaveOnHideoutLoad)
        {
            // Raid 탈출 성공: 저장된 stash + 현재 raid loot 합쳐서 저장
            MergeRaidLootIntoSavedStash();
            SaveManager.Save(SaveManager.CaptureCurrentState());
        }
        SaveManager.SaveOnHideoutLoad = false;
    }

    private void MergeRaidLootIntoSavedStash()
    {
        // 1. 현재 인벤토리(=raid loot)를 스냅샷
        var raidLoot = new System.Collections.Generic.List<InventorySlotSave>();
        if (Inventory.HasInstance)
        {
            for (int i = 0; i < Inventory.Instance.SlotCount; i++)
            {
                var slot = Inventory.Instance.GetSlot(i);
                if (slot == null || slot.data == null || slot.count <= 0) continue;
                raidLoot.Add(new InventorySlotSave
                {
                    slotIndex = i,
                    itemDataName = slot.data.itemName,
                    count = slot.count
                });
            }
        }

        // 2. 레이드 직전 stash를 save 파일에서 복원 (ApplySaveData가 인벤토리 클리어 후 복원)
        var preRaidSave = SaveManager.Load();
        if (preRaidSave != null) ApplySaveData(preRaidSave);

        // 3. raid loot을 TryAdd로 stash 위에 추가 (stack/empty slot 자동 처리)
        if (Inventory.HasInstance)
        {
            var registry = ItemDataRegistry.Get();
            foreach (var entry in raidLoot)
            {
                var data = registry != null ? registry.FindByName(entry.itemDataName) : null;
                if (data != null) Inventory.Instance.TryAdd(data, entry.count);
            }
        }
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
        // 저장된 stash 상태는 현재 인벤토리 그대로
        SaveManager.Save(SaveManager.CaptureCurrentState());
        EscapeItemRegistry.Reset();

        // 익스트랙션 룰: 레이드는 빈손으로 시작
        if (Inventory.HasInstance) Inventory.Instance.ClearAll();

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
