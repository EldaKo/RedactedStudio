using System;
using UnityEngine;

/// <summary>
/// 탈출 관련 이벤트 허브. 씬 내 여러 오브젝트가 직접 참조 없이 통신한다.
/// </summary>
public static class EscapeEvents
{
    /// <summary>열쇠를 새로 주웠을 때. (displayName, collectedCount, requiredCount)</summary>
    public static event Action<string, int, int> OnKeyPickup;

    /// <summary>출구에 진입했지만 열쇠가 부족할 때. (collectedCount, requiredCount)</summary>
    public static event Action<int, int> OnExitFailed;

    /// <summary>모든 열쇠를 갖고 출구에 진입해 클리어됐을 때.</summary>
    public static event Action OnEscapeSuccess;

    public static void NotifyKeyPickup(string name, int collected, int required)
        => OnKeyPickup?.Invoke(name, collected, required);

    public static void NotifyExitFailed(int collected, int required)
        => OnExitFailed?.Invoke(collected, required);

    public static void NotifyEscapeSuccess()
        => OnEscapeSuccess?.Invoke();
}
