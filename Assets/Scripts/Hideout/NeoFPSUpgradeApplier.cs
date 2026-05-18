using UnityEngine;
using NeoFPS;
using NeoFPS.ModularFirearms;
using System.Collections;
using System.Reflection;

public class NeoFPSUpgradeApplier : MonoBehaviour
{
    [Tooltip("방어구 시설 레벨별 내구도. index = 레벨. 헬멧/바디 동일하게 적용. 레벨 3 이상은 마지막 값 사용")]
    public int[] armorDurabilityByLevel = new int[] { 0, 25, 35, 55 };

    void Start()
    {
        StartCoroutine(ApplyUpgradesRoutine());
    }

    private IEnumerator ApplyUpgradesRoutine()
    {
        if (PlayerUpgradeManager.Instance == null) yield break;
        yield return new WaitForSeconds(0.5f);

        ApplyArmorUpgrade();
        ApplyWeaponUpgrade();
    }

    private void ApplyArmorUpgrade()
    {
        int level = PlayerUpgradeManager.Instance.armorLevel;
        int durability = GetArmorDurability(level);

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

        item.quantity = durability;
        Debug.Log($"[NeoFPS] {armor.name} 내구도={durability} (Lv{level}, invID={inventoryID})");
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
