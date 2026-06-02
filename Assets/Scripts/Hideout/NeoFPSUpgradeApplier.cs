using UnityEngine;
using NeoFPS;
using NeoFPS.ModularFirearms;
using System.Collections;
using System.Reflection;

public class NeoFPSUpgradeApplier : MonoBehaviour
{
    [Tooltip("방어구 시설 레벨별 최대 내구도. index = 레벨. 헬멧/바디 동일하게 적용. 레벨 3 이상은 마지막 값 사용")]
    public int[] armorDurabilityByLevel = new int[] { 0, 10, 20, 30 };

    [Tooltip("입장 시 자동으로 인벤토리에 추가/착용할 방어구 아이템 프리팹 (Armour_Body, Armour_Helmet 등)")]
    public GameObject[] armorItemPrefabs;

    [Tooltip("체력 시설 레벨별 최대 체력. index = 레벨. 레벨 범위 밖은 마지막 값 사용")]
    public float[] healthByLevel = new float[] { 100f, 100f, 110f, 120f };

    void Start()
    {
        StartCoroutine(ApplyUpgradesRoutine());
    }

    private IEnumerator ApplyUpgradesRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (PlayerUpgradeManager.Instance == null) yield break;

        EquipArmorByLevel();
        ApplyHealthByLevel();
        EquipSelectedWeapon();
        ApplyWeaponUpgrade();
    }

    private void ApplyHealthByLevel()
    {
        int level = PlayerUpgradeManager.Instance.healthLevel;
        if (healthByLevel == null || healthByLevel.Length == 0) return;
        if (level < 1) level = 1;
        if (level >= healthByLevel.Length) level = healthByLevel.Length - 1;
        float maxHp = healthByLevel[level];

        var healthManagers = GetComponentsInChildren<BasicHealthManager>(true);
        foreach (var hm in healthManagers)
        {
            hm.healthMax = maxHp;
            hm.health = maxHp; // 레이드 시작 시 풀피
            Debug.Log($"[NeoFPS] 최대 체력={maxHp} (Lv{level})");
        }
    }

    private void EquipSelectedWeapon()
    {
        var db = WeaponData.Get();
        if (db == null) return;

        var weapon = db.GetEquippedOrFirst();
        if (weapon == null || weapon.weaponPrefab == null) return;

        ICharacter character = GetComponent<ICharacter>() ?? GetComponentInParent<ICharacter>();
        if (character == null)
        {
            foreach (var c in FindObjectsOfType<MonoBehaviour>())
                if (c is ICharacter ic) { character = ic; break; }
        }

        IInventory inventory = character != null ? character.inventory : GetComponentInParent<IInventory>();
        if (inventory == null) { Debug.LogWarning("[NeoFPS] EquipWeapon: 인벤토리 없음"); return; }

        // 기본 장착 무기(리볼버 등)가 드랍되지 않도록, 기존 화기를 먼저 제거
        var existing = inventory.GetItems();
        if (existing != null)
        {
            var toRemove = new System.Collections.Generic.List<IInventoryItem>();
            foreach (var it in existing)
                if (it != null && it.GetComponent<ModularFirearm>() != null)
                    toRemove.Add(it);
            foreach (var it in toRemove)
                inventory.RemoveItem(it);
        }

        inventory.AddItemFromPrefab(weapon.weaponPrefab);

        // 지정 무기를 즉시 wield (Firearm 카테고리 = 슬롯0)
        if (character != null && character.quickSlots != null)
            character.quickSlots.SelectSlot(0);

        Debug.Log($"[NeoFPS] 무기 지급: {weapon.displayName}");
    }

    private void EquipArmorByLevel()
    {
        int level = PlayerUpgradeManager.Instance.armorLevel;
        int durability = GetArmorDurability(level);

        ICharacter character = GetComponent<ICharacter>() ?? GetComponentInParent<ICharacter>();
        if (character == null)
        {
            foreach (var c in FindObjectsOfType<MonoBehaviour>())
                if (c is ICharacter ic) { character = ic; break; }
        }

        IInventory inventory = character != null ? character.inventory : GetComponentInParent<IInventory>();
        if (inventory == null) { Debug.LogWarning("[NeoFPS] EquipArmor: 인벤토리 없음"); return; }

        // 레벨과 무관하게 방어구 아이템을 인벤토리에 직접 추가 (픽업 불필요)
        if (armorItemPrefabs != null)
        {
            foreach (var prefab in armorItemPrefabs)
            {
                if (prefab == null) continue;
                inventory.AddItemFromPrefab(prefab);
            }
        }

        // 각 방어구 핸들러의 내구도를 레벨에 맞게 설정
        ArmouredDamageHandler[] handlers = GetComponentsInChildren<ArmouredDamageHandler>(true);
        foreach (var h in handlers) ApplyArmorDurability(h, durability, level);
    }

    private int GetArmorDurability(int level)
    {
        if (armorDurabilityByLevel == null || armorDurabilityByLevel.Length == 0) return 0;
        if (level < 1) level = 1;
        if (level >= armorDurabilityByLevel.Length) level = armorDurabilityByLevel.Length - 1;
        return armorDurabilityByLevel[level];
    }

    private static readonly FieldInfo s_InventoryIDField =
        typeof(ArmouredDamageHandler).GetField("m_InventoryID", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo s_InventoryField =
        typeof(ArmouredDamageHandler).GetField("m_Inventory", BindingFlags.NonPublic | BindingFlags.Instance);

    private static void ApplyArmorDurability(ArmouredDamageHandler armor, int durability, int level)
    {
        if (s_InventoryIDField == null || s_InventoryField == null)
        {
            Debug.LogWarning("[NeoFPS] ArmouredDamageHandler 리플렉션 필드 누락");
            return;
        }

        int inventoryID = (int)s_InventoryIDField.GetValue(armor);
        var inventory = s_InventoryField.GetValue(armor) as IInventory
                        ?? armor.GetComponentInParent<IInventory>();
        if (inventory == null)
        {
            Debug.LogWarning($"[NeoFPS] {armor.name} 갑옷 인벤토리 없음");
            return;
        }

        var item = inventory.GetItem(inventoryID);
        if (item == null)
        {
            Debug.LogWarning($"[NeoFPS] {armor.name} 갑옷 아이템(ID={inventoryID}) 인벤토리에 없음");
            return;
        }

        // 최대치를 레벨 내구도로 변경 → HUD 표시가 "현재/레벨" 형식이 됨
        var maxField = item.GetType().GetField("m_MaxQuantity", BindingFlags.NonPublic | BindingFlags.Instance);
        if (maxField != null) maxField.SetValue(item, durability);

        item.quantity = durability;
        Debug.Log($"[NeoFPS] {armor.name} 내구도={durability}/{durability} (Lv{level}, invID={inventoryID})");
    }

    private void ApplyWeaponUpgrade()
    {
        IInventory inventory = GetComponent<IInventory>();
        if (inventory == null) return;

        foreach (var item in inventory.GetItems())
        {
            ModularFirearm firearm = item.GetComponent<ModularFirearm>();
            if (firearm == null || firearm.ammo == null) continue;

            string cleanName = firearm.name.Replace("(Clone)", "").Trim();
            int level = PlayerUpgradeManager.Instance.GetWeaponLevel(cleanName);
            if (level <= 1) continue;

            Component ammoEffect = firearm.ammo.effect as Component;
            if (ammoEffect == null) continue;

            FieldInfo damageField = ammoEffect.GetType().GetField("m_Damage", BindingFlags.NonPublic | BindingFlags.Instance);
            if (damageField == null) continue;

            float baseDamage = (float)damageField.GetValue(ammoEffect);
            float newDamage = baseDamage + ((level - 1) * 10f);
            damageField.SetValue(ammoEffect, newDamage);

            Debug.Log($"[NeoFPS] {cleanName} 데미지 {baseDamage} → {newDamage}");
        }
    }
}
