using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 무기 시설. 좌우 화살표로 무기를 넘겨보고, 잠긴 무기는 재료 소모해 해금,
/// 해금된 무기는 장착. 해금된 무기는 재료/해금 UI를 숨기고 장착 버튼만 표시.
/// </summary>
public class WeaponBench : MonoBehaviour
{
    [System.Serializable]
    public class MaterialSlot
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI countText;
    }

    [Header("UI 패널")]
    public GameObject panelRoot;
    public Button closeButton;

    [Header("무기 표시")]
    public Image weaponIcon;
    public TextMeshProUGUI weaponNameText;
    public Button prevButton;
    public Button nextButton;

    [Header("재료 슬롯 (잠긴 무기 해금 비용 표시)")]
    public MaterialSlot[] materialSlots;

    [Header("버튼")]
    public Button unlockButton;
    public TextMeshProUGUI unlockButtonText;
    public Button equipButton;
    public TextMeshProUGUI equipButtonText;

    [Header("카메라 연출")]
    public Transform cameraViewPoint;

    private int _index;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        if (prevButton != null) prevButton.onClick.AddListener(() => Step(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        if (unlockButton != null) unlockButton.onClick.AddListener(OnUnlockClicked);
        if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
    }

    private void OnMouseDown()
    {
        // UI 위를 클릭한 경우(패널 버튼 등) 3D 시설 클릭 무시
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null && es.IsPointerOverGameObject()) return;

        if (HideoutUIManager.Instance != null)
            HideoutUIManager.Instance.OpenWeaponBench(this);
    }

    private void OnCloseClicked()
    {
        if (HideoutUIManager.Instance != null) HideoutUIManager.Instance.CloseAll();
        else ClosePanel();
    }

    public void OpenPanel()
    {
        var db = WeaponData.Get();
        // 현재 장착 무기를 시작 인덱스로
        if (db != null) { int idx = db.IndexOf(PlayerLoadout.EquippedWeapon); _index = idx >= 0 ? idx : 0; }
        Refresh();
        if (panelRoot != null) panelRoot.SetActive(true);
        if (cameraViewPoint != null && HideoutCamera.Instance != null)
            HideoutCamera.Instance.MoveToFacility(cameraViewPoint);
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Step(int dir)
    {
        var db = WeaponData.Get();
        if (db == null || db.weapons.Count == 0) return;
        int next = _index + dir;
        if (next < 0 || next > GetMaxReachableIndex(db)) return; // 경계 넘으면 이동 안 함 (순환 X)
        _index = next;
        Refresh();
    }

    // 탐색 가능한 최대 인덱스 = 해금된 무기 중 가장 뒤 + 1 (다음 해금 대상까지만)
    private int GetMaxReachableIndex(WeaponData db)
    {
        int maxUnlocked = 0;
        for (int i = 0; i < db.weapons.Count; i++)
            if (db.weapons[i] != null && PlayerLoadout.IsUnlocked(db.weapons[i].id))
                maxUnlocked = i;
        return Mathf.Min(maxUnlocked + 1, db.weapons.Count - 1);
    }

    private void Refresh()
    {
        var db = WeaponData.Get();
        if (db == null || db.weapons.Count == 0) return;

        int maxReachable = GetMaxReachableIndex(db);
        _index = Mathf.Clamp(_index, 0, maxReachable);

        var weapon = db.weapons[_index];
        if (weapon == null) return;

        if (weaponIcon != null) { weaponIcon.sprite = weapon.weaponIcon; weaponIcon.enabled = weapon.weaponIcon != null; }
        if (weaponNameText != null) weaponNameText.text = weapon.displayName;

        // 화살표: 경계에서 비활성 (순환 없음, 잠긴 무기 너머로 못 감)
        if (prevButton != null) prevButton.interactable = _index > 0;
        if (nextButton != null) nextButton.interactable = _index < maxReachable;

        bool unlocked = PlayerLoadout.IsUnlocked(weapon.id);
        bool equipped = PlayerLoadout.EquippedWeapon == weapon.id;
        bool prevUnlocked = (_index == 0) || PlayerLoadout.IsUnlocked(db.weapons[_index - 1].id);

        if (unlocked)
        {
            // 해금됨: 재료/해금 숨기고 장착 버튼만
            ShowMaterials(null);
            if (unlockButton != null) unlockButton.gameObject.SetActive(false);
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(true);
                equipButton.interactable = !equipped;
                if (equipButtonText != null) equipButtonText.text = equipped ? "장착중" : "장착";
            }
        }
        else
        {
            // 잠김: 재료 + 해금 버튼
            ShowMaterials(weapon.unlockMaterials);
            if (equipButton != null) equipButton.gameObject.SetActive(false);
            if (unlockButton != null)
            {
                unlockButton.gameObject.SetActive(true);
                bool affordable = prevUnlocked && CanAfford(weapon);
                unlockButton.interactable = affordable;
                if (unlockButtonText != null)
                    unlockButtonText.text = prevUnlocked ? "해금" : "이전 무기 필요";
            }
        }
    }

    private void ShowMaterials(List<MaterialRequirement> reqs)
    {
        if (materialSlots == null) return;
        for (int i = 0; i < materialSlots.Length; i++)
        {
            var slot = materialSlots[i];
            if (slot == null) continue;

            bool active = reqs != null && i < reqs.Count && reqs[i] != null && reqs[i].material != null;
            if (slot.root != null) slot.root.SetActive(active);
            if (!active) continue;

            var req = reqs[i];
            int required = req.baseAmount;
            int have = Inventory.HasInstance ? Inventory.Instance.GetTotalCount(req.material) : 0;
            string color = have >= required ? "green" : "red";

            if (slot.icon != null) { slot.icon.sprite = req.material.icon; slot.icon.enabled = req.material.icon != null; }
            if (slot.countText != null) slot.countText.text = $"<color={color}>{have}/{required}</color>";
        }
    }

    private bool CanAfford(WeaponEntry weapon)
    {
        if (weapon.unlockMaterials == null || weapon.unlockMaterials.Count == 0) return true;
        if (!Inventory.HasInstance) return false;
        foreach (var req in weapon.unlockMaterials)
        {
            if (req == null || req.material == null) continue;
            if (Inventory.Instance.GetTotalCount(req.material) < req.baseAmount) return false;
        }
        return true;
    }

    private void OnUnlockClicked()
    {
        var db = WeaponData.Get();
        if (db == null) return;
        var weapon = db.weapons[_index];
        if (weapon == null || PlayerLoadout.IsUnlocked(weapon.id)) return;

        bool prevUnlocked = (_index == 0) || PlayerLoadout.IsUnlocked(db.weapons[_index - 1].id);
        if (!prevUnlocked || !CanAfford(weapon)) return;

        if (weapon.unlockMaterials != null && Inventory.HasInstance)
            foreach (var req in weapon.unlockMaterials)
                if (req != null && req.material != null)
                    Inventory.Instance.TryConsume(req.material, req.baseAmount);

        PlayerLoadout.Unlock(weapon.id);
        PlayerLoadout.Equip(weapon.id); // 해금 시 자동 장착
        Debug.Log($"[WeaponBench] {weapon.displayName} 해금 + 장착");
        Refresh();
    }

    private void OnEquipClicked()
    {
        var db = WeaponData.Get();
        if (db == null) return;
        var weapon = db.weapons[_index];
        if (weapon == null || !PlayerLoadout.IsUnlocked(weapon.id)) return;

        PlayerLoadout.Equip(weapon.id);
        Refresh();
    }
}
