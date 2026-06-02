using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponLevelData
{
    public string weaponName;
    public int level = 1;
}

public class PlayerUpgradeManager : MonoBehaviour
{
    public static PlayerUpgradeManager Instance;

    [Header("캐릭터 현재 상태")]
    public int armorLevel = 1;
    public int healthLevel = 1;

    [Header("무기 레벨 목록 (인스펙터 확인용)")]
    public List<WeaponLevelData> weaponLevelList = new List<WeaponLevelData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 이동 시 파괴 방지
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public int GetWeaponLevel(string weaponName)
    {
        var data = weaponLevelList.Find(x => x.weaponName == weaponName);
        return (data != null) ? data.level : 1;
    }

    public void UpgradeWeaponLevel(string weaponName)
    {
        var data = weaponLevelList.Find(x => x.weaponName == weaponName);
        if (data != null)
        {
            data.level++;
        }
        else
        {
            weaponLevelList.Add(new WeaponLevelData { weaponName = weaponName, level = 2 });
        }
    }
}