using UnityEngine;
using NeoFPS;
using NeoFPS.ModularFirearms;
using System.Collections;
using System.Reflection; 

public class NeoFPSUpgradeApplier : MonoBehaviour
{
    void Start()
    {
        ApplyArmorUpgrade();
        StartCoroutine(ApplyWeaponUpgradeRoutine());
    }

    private void ApplyArmorUpgrade()
    {
        if (PlayerUpgradeManager.Instance == null) return;
        int currentArmorLevel = PlayerUpgradeManager.Instance.armorLevel;
        if (currentArmorLevel <= 1) return; 

        ArmouredDamageHandler[] armorHandlers = GetComponentsInChildren<ArmouredDamageHandler>(true);
        foreach (var armor in armorHandlers) ApplyArmorLevelTo(armor, currentArmorLevel);
    }

    private static void ApplyArmorLevelTo(ArmouredDamageHandler armor, int level)
    {
        var handlerType = typeof(ArmouredDamageHandler);
        var mitigationField = handlerType.GetField("m_DamageMitigation", BindingFlags.NonPublic | BindingFlags.Instance);
        var multiplierField = handlerType.GetField("m_ArmourDamageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance);

        if (mitigationField != null)
        {
            float baseMitigation = (float)mitigationField.GetValue(armor);
            mitigationField.SetValue(armor, Mathf.Clamp01(baseMitigation + (level - 1) * 0.1f));
        }
        if (multiplierField != null)
        {
            float baseMultiplier = (float)multiplierField.GetValue(armor);
            multiplierField.SetValue(armor, Mathf.Max(0f, baseMultiplier - (level - 1) * 0.1f));
        }

        Debug.Log($"[NeoFPS] {armor.name} 갑옷 레벨 {level} 적용 완료");
    }

    private IEnumerator ApplyWeaponUpgradeRoutine()
    {
        if (PlayerUpgradeManager.Instance == null) yield break;

        yield return new WaitForSeconds(0.5f);

        IInventory inventory = GetComponent<IInventory>();
        if (inventory != null)
        {
            foreach (var item in inventory.GetItems())
            {
                ModularFirearm firearm = item.GetComponent<ModularFirearm>();
                if (firearm != null && firearm.ammo != null)
                {
                    // 이름에서 (Clone)을 제거하여 원본 프리팹 이름으로 맞춥니다.
                    string cleanName = firearm.name.Replace("(Clone)", "").Trim();
                    
                    // 매니저에 이 무기의 업그레이드 레벨이 몇인지 물어봅니다.
                    int level = PlayerUpgradeManager.Instance.GetWeaponLevel(cleanName);
                    
                    // 2레벨 이상일 경우에만 데미지 증가 적용
                    if (level > 1)
                    {
                        Component ammoEffect = firearm.ammo.effect as Component;
                        if (ammoEffect != null)
                        {
                            FieldInfo damageField = ammoEffect.GetType().GetField("m_Damage", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (damageField != null)
                            {
                                float baseDamage = (float)damageField.GetValue(ammoEffect);
                                float newDamage = baseDamage + ((level - 1) * 10f); // 1레벨당 10씩 증가
                                damageField.SetValue(ammoEffect, newDamage);
                                
                                Debug.Log($"[NeoFPS] {cleanName} 데미지 업그레이드 완료! ({baseDamage} -> {newDamage})");
                            }
                        }
                    }
                }
            }
        }
    }
}