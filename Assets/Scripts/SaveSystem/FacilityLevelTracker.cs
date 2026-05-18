using System.Collections.Generic;

public static class FacilityLevelTracker
{
    private static Dictionary<string, int> _levels = new Dictionary<string, int>();

    public static int GetLevel(string facilityName, int fallback)
    {
        if (string.IsNullOrEmpty(facilityName)) return fallback;
        return _levels.TryGetValue(facilityName, out int v) ? v : fallback;
    }

    public static void SetLevel(string facilityName, int level)
    {
        if (string.IsNullOrEmpty(facilityName)) return;
        _levels[facilityName] = level;
    }

    public static void Clear() => _levels.Clear();

    public static IEnumerable<KeyValuePair<string, int>> All => _levels;
}
