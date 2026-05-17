using System;
using System.Collections.Generic;
using UnityEngine;

public static class EscapeItemRegistry
{
    private static readonly HashSet<string> collected = new HashSet<string>();
    public static int RequiredCount { get; private set; } = 3;

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
            // ==========================================
            // [추가] 열쇠를 먹으면 은신처(ItemHideout)에도 영구 세이브!
            // ==========================================
            if (ItemHideout.Instance != null)
            {
                ItemHideout.Instance.AddKey(id);
            }

            Debug.Log($"[Escape] 아이템 수집: {id} ({collected.Count}/{RequiredCount})");
            OnChanged?.Invoke(collected.Count, RequiredCount);
        }
        return added;
    }

    // ==========================================
    // [추가] 씬이 시작될 때 은신처에서 세이브된 열쇠를 불러오는 함수
    // ==========================================
    public static void RestoreSavedKeys()
    {
        if (ItemHideout.Instance == null) return;

        bool isRestored = false;
        foreach (string savedKey in ItemHideout.Instance.collectedKeys)
        {
            if (collected.Add(savedKey))
            {
                isRestored = true;
            }
        }

        // 불러온 열쇠가 하나라도 있다면 UI 및 기믹(이벤트) 업데이트
        if (isRestored)
        {
            Debug.Log($"[Escape] 세이브된 열쇠 불러오기 완료 ({collected.Count}/{RequiredCount})");
            OnChanged?.Invoke(collected.Count, RequiredCount);
        }
    }

    public static bool HasAll() => collected.Count >= RequiredCount;
    public static int CollectedCount => collected.Count;
    public static bool Has(string id) => collected.Contains(id);
}