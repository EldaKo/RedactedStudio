using System.Collections.Generic;

/// <summary>
/// 클리어한 스테이지(씬 이름)를 전역 추적. 저장/로드와 연동.
/// </summary>
public static class StageProgress
{
    private static readonly HashSet<string> _cleared = new HashSet<string>();

    public static bool IsCleared(string sceneName)
        => !string.IsNullOrEmpty(sceneName) && _cleared.Contains(sceneName);

    public static void MarkCleared(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName)) _cleared.Add(sceneName);
    }

    public static void Clear() => _cleared.Clear();

    public static IEnumerable<string> All => _cleared;

    public static void SetAll(IEnumerable<string> sceneNames)
    {
        _cleared.Clear();
        if (sceneNames == null) return;
        foreach (var s in sceneNames)
            if (!string.IsNullOrEmpty(s)) _cleared.Add(s);
    }
}
