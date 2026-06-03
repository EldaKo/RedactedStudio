using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponEntry
{
    [Tooltip("저장/식별용 고유 id (예: revolver)")]
    public string id;
    [Tooltip("UI 표시 이름")]
    public string displayName;
    [Tooltip("무기 시설 UI에 표시할 아이콘 (NeoFPS HUD_WeaponIcons 등)")]
    public Sprite weaponIcon;
    [Tooltip("레이드 입장 시 플레이어 인벤토리에 지급할 무기 프리팹 (Firearm_*_Swappable)")]
    public GameObject weaponPrefab;
    [Tooltip("이 무기 탄종의 적 드롭용 탄약 픽업 프리팹 (Pickup_Ammo*)")]
    public GameObject ammoPickupPrefab;
    [Tooltip("이 무기를 해금하는 데 필요한 재료 (방어구 시설과 동일 형식). 비우면 무료")]
    public List<MaterialRequirement> unlockMaterials = new List<MaterialRequirement>();
}

[CreateAssetMenu(menuName = "Inventory/Weapon Database", fileName = "WeaponDatabase")]
public class WeaponData : ScriptableObject
{
    [Tooltip("무기 목록. 순서 = 단계별 해금 순서 (앞 무기를 해금해야 다음 해금 가능)")]
    public List<WeaponEntry> weapons = new List<WeaponEntry>();

    private static WeaponData _cached;

    public static WeaponData Get()
    {
        if (_cached == null) _cached = Resources.Load<WeaponData>("WeaponDatabase");
        return _cached;
    }

    public WeaponEntry FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var w in weapons)
            if (w != null && w.id == id) return w;
        return null;
    }

    // 현재 장착 무기, 없으면 첫 무기로 폴백
    public WeaponEntry GetEquippedOrFirst()
    {
        var w = FindById(PlayerLoadout.EquippedWeapon);
        if (w != null) return w;
        return weapons.Count > 0 ? weapons[0] : null;
    }

    public int IndexOf(string id)
    {
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i] != null && weapons[i].id == id) return i;
        return -1;
    }
}
