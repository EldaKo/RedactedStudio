using System.Collections.Generic;

/// <summary>
/// 해금한 무기 목록과 현재 장착 무기를 전역 추적. 저장/로드와 연동.
/// </summary>
public static class PlayerLoadout
{
    private static readonly HashSet<string> _unlocked = new HashSet<string>();
    private static string _equipped = "";

    public static string EquippedWeapon => _equipped;

    public static bool IsUnlocked(string id)
        => !string.IsNullOrEmpty(id) && _unlocked.Contains(id);

    public static void Unlock(string id)
    {
        if (!string.IsNullOrEmpty(id)) _unlocked.Add(id);
    }

    public static void Equip(string id)
    {
        if (IsUnlocked(id)) _equipped = id;
    }

    public static IEnumerable<string> AllUnlocked => _unlocked;

    public static void Clear()
    {
        _unlocked.Clear();
        _equipped = "";
    }

    public static void SetAll(IEnumerable<string> unlocked, string equipped)
    {
        _unlocked.Clear();
        if (unlocked != null)
            foreach (var u in unlocked)
                if (!string.IsNullOrEmpty(u)) _unlocked.Add(u);
        _equipped = equipped ?? "";
    }
}
