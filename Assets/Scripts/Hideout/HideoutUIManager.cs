using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HideoutUIManager : MonoBehaviour
{
    public static HideoutUIManager Instance;

    [System.Serializable]
    public class StageInfo
    {
        public string displayName = "스테이지";
        public string sceneName = "DEMO City Ruins";
        [Tooltip("이 스테이지를 해금하려면 클리어해야 하는 선행 스테이지의 씬 이름. 비우면 항상 해금")]
        public string requiresClearOf = "";
    }

    [Header("공통 버튼 (모든 시설 공유)")]
    public Button exitToMainButton;
    [Tooltip("게임을 저장하고 메인 화면으로 돌아감")]
    public Button saveAndExitButton;
    [Tooltip("스테이지 선택 패널을 여는 버튼")]
    public Button enterRaidButton;

    [Header("스테이지 선택")]
    [Tooltip("Enter_Raid 클릭 시 열리는 스테이지 선택 패널 (기본 비활성)")]
    public GameObject stageSelectPanel;
    [Tooltip("스테이지 버튼들이 생성될 부모 (예: 세로 레이아웃 그룹)")]
    public Transform stageButtonContainer;
    [Tooltip("복제 대상이 될 스테이지 버튼 템플릿")]
    public Button stageButtonTemplate;
    [Tooltip("스테이지 선택 패널 닫기 버튼")]
    public Button stageSelectCloseButton;
    [Tooltip("선택 가능한 스테이지 목록")]
    public List<StageInfo> stages = new List<StageInfo>();

    [Header("씬 이름")]
    public string mainScreenSceneName = "mainScreen";

    private readonly List<GameObject> _spawnedStageButtons = new List<GameObject>();

    void Awake()
    {
        Instance = this;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (exitToMainButton != null) exitToMainButton.onClick.AddListener(GoToMainScreen);
        if (saveAndExitButton != null) saveAndExitButton.onClick.AddListener(SaveAndExit);
        if (enterRaidButton != null) enterRaidButton.onClick.AddListener(OpenStageSelect);
        if (stageSelectCloseButton != null) stageSelectCloseButton.onClick.AddListener(CloseStageSelect);

        if (stageButtonTemplate != null) stageButtonTemplate.gameObject.SetActive(false);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
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
            StageProgress.SetAll(SaveManager.PendingLoad.clearedStages);
            PlayerLoadout.SetAll(SaveManager.PendingLoad.unlockedWeapons, SaveManager.PendingLoad.equippedWeapon);
            SaveManager.PendingLoad = null;
        }
        else if (SaveManager.SaveOnHideoutLoad)
        {
            // Raid 탈출 성공: 저장된 stash + 현재 raid loot 합쳐서 저장
            // (StageProgress는 ExitZone에서 이미 MarkCleared됨 → 그대로 저장에 포함)
            MergeRaidLootIntoSavedStash();
            SaveManager.Save(SaveManager.CaptureCurrentState());
        }
        SaveManager.SaveOnHideoutLoad = false;

        EnsureDefaultWeapon();
    }

    // 장착 무기가 없으면 첫 무기를 자동 해금·장착 (어느 경로로 진입해도 무기 보장)
    private void EnsureDefaultWeapon()
    {
        if (!string.IsNullOrEmpty(PlayerLoadout.EquippedWeapon)) return;
        var db = WeaponData.Get();
        if (db == null || db.weapons.Count == 0 || db.weapons[0] == null) return;
        PlayerLoadout.Unlock(db.weapons[0].id);
        PlayerLoadout.Equip(db.weapons[0].id);
        Debug.Log($"[Loadout] 기본 무기 자동 장착: {db.weapons[0].displayName}");
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
            PlayerUpgradeManager.Instance.healthLevel = data.healthLevel;
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

    public void OpenStageSelect()
    {
        if (stageSelectPanel != null) stageSelectPanel.SetActive(true);
        SetHubButtonsVisible(false);
        RebuildStageButtons();
    }

    public void CloseStageSelect()
    {
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        SetHubButtonsVisible(true);
    }

    private void RebuildStageButtons()
    {
        foreach (var go in _spawnedStageButtons)
            if (go != null) Destroy(go);
        _spawnedStageButtons.Clear();

        if (stageButtonTemplate == null || stageButtonContainer == null) return;

        foreach (var stage in stages)
        {
            if (stage == null) continue;

            var btnObj = Instantiate(stageButtonTemplate.gameObject, stageButtonContainer);
            btnObj.SetActive(true);
            _spawnedStageButtons.Add(btnObj);

            bool unlocked = string.IsNullOrEmpty(stage.requiresClearOf)
                            || StageProgress.IsCleared(stage.requiresClearOf);

            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = unlocked ? stage.displayName : $"{stage.displayName} (잠김)";

            string sceneName = stage.sceneName;
            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = unlocked;
                if (unlocked)
                    btn.onClick.AddListener(() => EnterRaidStage(sceneName));
            }
        }
    }

    public void EnterRaidStage(string sceneName)
    {
        // 저장된 stash 상태는 현재 인벤토리 그대로
        SaveManager.Save(SaveManager.CaptureCurrentState());
        EscapeItemRegistry.Reset();

        // 익스트랙션 룰: 레이드는 빈손으로 시작
        if (Inventory.HasInstance) Inventory.Instance.ClearAll();

        SceneManager.LoadScene(sceneName);
    }

    public void OpenFacility(FacilityScript facility)
    {
        foreach (var f in FindObjectsOfType<FacilityScript>(true))
        {
            if (f != facility) f.Close();
        }
        foreach (var wb in FindObjectsOfType<WeaponBench>(true))
            wb.ClosePanel();
        facility.Open();
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        SetHubButtonsVisible(false);
    }

    public void OpenWeaponBench(WeaponBench bench)
    {
        foreach (var f in FindObjectsOfType<FacilityScript>(true))
            f.Close();
        foreach (var wb in FindObjectsOfType<WeaponBench>(true))
            if (wb != bench) wb.ClosePanel();
        bench.OpenPanel();
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        SetHubButtonsVisible(false);
    }

    public void CloseAll()
    {
        foreach (var f in FindObjectsOfType<FacilityScript>(true))
            f.Close();
        foreach (var wb in FindObjectsOfType<WeaponBench>(true))
            wb.ClosePanel();

        if (HideoutCamera.Instance != null) HideoutCamera.Instance.ReturnToTopView();
        SetHubButtonsVisible(true);
    }

    private void SetHubButtonsVisible(bool visible)
    {
        if (saveAndExitButton != null) saveAndExitButton.gameObject.SetActive(visible);
        if (enterRaidButton != null) enterRaidButton.gameObject.SetActive(visible);
    }

    public void GoToMainScreen()
    {
        SceneManager.LoadScene(mainScreenSceneName);
    }
}
