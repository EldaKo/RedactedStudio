using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using NeoFPS;
using NeoFPS.ModularFirearms;

public class WorkbenchUIManager : MonoBehaviour
{
    [Header("플레이어 프리팹 (필수 연결)")]
    public GameObject playerPrefab; 

    [Header("방어구 업그레이드 (파란 열쇠 소모)")]
    public TextMeshProUGUI armorInfoText;
    public TextMeshProUGUI armorCostText;
    public Button armorUpgradeBtn;

    [Header("무기 업그레이드 (빨간 열쇠 소모)")]
    public TMP_Dropdown weaponDropdown; 
    public TextMeshProUGUI weaponInfoText;
    public TextMeshProUGUI weaponCostText;
    public Button weaponUpgradeBtn;

    [Header("기타 UI")]
    public Button closeButton;
    private List<ModularFirearm> availableWeapons = new List<ModularFirearm>();

    private void Awake()
    {
        if (armorUpgradeBtn) armorUpgradeBtn.onClick.AddListener(OnClickArmorUpgrade);
        if (weaponUpgradeBtn) weaponUpgradeBtn.onClick.AddListener(OnClickWeaponUpgrade);
        if (closeButton) closeButton.onClick.AddListener(CloseWorkbenchUI);
        if (weaponDropdown != null) weaponDropdown.onValueChanged.AddListener(delegate { UpdateUI(); });
    }

    private void OnEnable()
    {
        // ========================================================
        // [안전장치] 첫 게임 시작 시 강제 활성화 방지
        // 유니티가 시작되는 첫 프레임(FrameCount 2 이하)에 다른 매니저에 의해 
        // 억지로 켜진다면, 화면에 보이기 전에 즉시 꺼버립니다.
        // ========================================================
        if (Time.frameCount <= 2)
        {
            gameObject.SetActive(false);
            return;
        }

        // 정상적으로 게임 플레이 중에 버튼을 눌러서 열었을 때만 아래 기능이 실행됩니다.
        LoadWeaponsFromPrefab(); 
        Invoke("UpdateUI", 0.1f); 
    }

    public void UpdateUI()
    {
        if (PlayerUpgradeManager.Instance == null || ItemHideout.Instance == null) return;

        // 1. 방어구 업데이트 (파란 열쇠 'blueKey' 필요)
        int currentArmor = PlayerUpgradeManager.Instance.armorLevel;
        bool hasBlueKey = ItemHideout.Instance.HasKey("blueKey");
        
        armorInfoText.text = $"방어구 레벨: {currentArmor}";
        armorCostText.text = hasBlueKey ? "<color=green>필요 아이템: 파란 열쇠 (보유 중)</color>" : "<color=red>필요 아이템: 파란 열쇠 (없음)</color>";
        armorUpgradeBtn.interactable = hasBlueKey;

        // 2. 무기 업데이트 (빨간 열쇠 'redKey' 필요)
        if (availableWeapons.Count > 0 && weaponDropdown != null)
        {
            string weaponName = availableWeapons[weaponDropdown.value].name.Replace("(Clone)", "").Trim();
            int weaponLevel = PlayerUpgradeManager.Instance.GetWeaponLevel(weaponName);
            bool hasRedKey = ItemHideout.Instance.HasKey("redKey");

            weaponInfoText.text = $"{weaponName} 레벨: {weaponLevel}";
            weaponCostText.text = hasRedKey ? "<color=green>필요 아이템: 빨간 열쇠 (보유 중)</color>" : "<color=red>필요 아이템: 빨간 열쇠 (없음)</color>";
            weaponUpgradeBtn.interactable = hasRedKey;
        }
    }

    private void OnClickArmorUpgrade()
    {
        // 파란 열쇠 소모 후 레벨업
        if (ItemHideout.Instance.HasKey("blueKey"))
        {
            ItemHideout.Instance.RemoveKey("blueKey");
            PlayerUpgradeManager.Instance.armorLevel++;
            Debug.Log("파란 열쇠를 사용하여 방어구를 업그레이드했습니다!");
            UpdateUI();
        }
    }

    private void OnClickWeaponUpgrade()
    {
        if (availableWeapons.Count == 0) return;

        string weaponName = availableWeapons[weaponDropdown.value].name.Replace("(Clone)", "").Trim();
        
        // 빨간 열쇠 소모 후 레벨업
        if (ItemHideout.Instance.HasKey("redKey"))
        {
            ItemHideout.Instance.RemoveKey("redKey");
            PlayerUpgradeManager.Instance.UpgradeWeaponLevel(weaponName);
            Debug.Log($"{weaponName}을(를) 빨간 열쇠로 강화했습니다!");
            UpdateUI();
        }
    }

    // NeoFPS 플레이어 프리팹에서 무기 목록을 추출하는 로직
    private void LoadWeaponsFromPrefab() 
    { 
        if (weaponDropdown == null || playerPrefab == null) return;

        availableWeapons.Clear();
        weaponDropdown.ClearOptions();
        List<string> options = new List<string>();

        var inventory = playerPrefab.GetComponent("FpsInventorySwappable");
        if (inventory != null)
        {
            FieldInfo field = inventory.GetType().GetField("m_StartingItems", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                System.Array items = field.GetValue(inventory) as System.Array;
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item == null) continue;
                        var firearm = (item as Component).GetComponent<ModularFirearm>();
                        if (firearm != null)
                        {
                            availableWeapons.Add(firearm);
                            options.Add(firearm.name.Replace("(Clone)", "").Trim());
                        }
                    }
                }
            }
        }
        weaponDropdown.AddOptions(options);
    }

    // ========================================================
    // [수정] 닫기 버튼을 누를 때 카메라 줌아웃 연동
    // ========================================================
    private void CloseWorkbenchUI() 
    { 
        // 1. 작업대 UI 화면을 비활성화합니다.
        gameObject.SetActive(false); 

        // 2. 만약 작업대를 닫으면서 이전의 시설 선택 UI(HideoutUIPanel)를 다시 켜고 싶다면 
        //    아래 코드의 주석(//)을 해제하여 사용하세요.
        // if (HideoutUIManager.Instance != null && HideoutUIManager.Instance.uiPanel != null)
        //     HideoutUIManager.Instance.uiPanel.SetActive(true);

        // 3. 은신처 카메라를 다시 기본 전체보기(탑뷰) 위치로 부드럽게 되돌립니다.
        if (HideoutCamera.Instance != null)
        {
            HideoutCamera.Instance.ReturnToTopView();
        }
        else
        {
            Debug.LogWarning("HideoutCamera.Instance를 찾을 수 없어서 줌아웃을 실행할 수 없습니다.");
        }
    }
}