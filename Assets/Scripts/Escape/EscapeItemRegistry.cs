using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탈출 아이템 수집 상태를 전역으로 관리하는 정적 레지스트리.
/// 씬 로드 시 자동으로 초기화된다.
/// </summary>
public static class EscapeItemRegistry
{
    private static readonly HashSet<string> collected = new HashSet<string>();
    public static int RequiredCount { get; private set; } = 3;

    /// <summary>아이템 수집 상태가 변할 때 호출. (collected, required)</summary>
    public static event Action<int, int> OnChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        collected.Clear();
        RequiredCount = 3;
    }

    public static void SetRequiredCount(int count)
    {
        RequiredCount = Mathf.Max(0, count);
        OnChanged?.Invoke(collected.Count, RequiredCount);
    }

    public static bool Collect(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        bool added = collected.Add(id);
        if (added)
        {
            Debug.Log($"[Escape] 아이템 수집: {id} ({collected.Count}/{RequiredCount})");
            OnChanged?.Invoke(collected.Count, RequiredCount);
        }
        return added;
    }

    public static bool HasAll() => collected.Count >= RequiredCount;
    public static int CollectedCount => collected.Count;
    public static bool Has(string id) => collected.Contains(id);

    public static void Reset()
    {
        collected.Clear();
        OnChanged?.Invoke(0, RequiredCount);
    }
}
