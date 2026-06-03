using System;
using UnityEngine;

/// <summary>
/// 현재 보유 키 조합으로 KeyState(A~F)를 판별하고 이벤트를 발행한다.
///
/// ── 상태 정의 ──────────────────────────────────────────────────────────
///  A : 빨강 + 초록   (redKey  + greenKey)
///  B : 빨강 + 파랑   (redKey  + blueKey)
///  C : 초록 + 파랑   (greenKey + blueKey)
///  D : 빨강만        (redKey  only)
///  E : 초록만        (greenKey only)
///  F : 파랑만        (blueKey  only)
///  None : 아무 키도 없음 / 3개 전부 보유(탈출 조건)
/// ───────────────────────────────────────────────────────────────────────
///
/// 나중에 선택지를 붙이려면:
///   1. ChoicePresenter(MonoBehaviour) 를 만들고 OnStateChanged 를 구독한다.
///   2. 받은 KeyState 에 따라 switch 로 다른 선택지 UI 패널을 활성화한다.
///   3. 유저 선택 결과는 ChoiceOutcomeHandler 같은 별도 클래스에서 처리한다.
/// </summary>
public static class KeyStateEvaluator
{
    // ── 상태 열거형 ────────────────────────────────────────────────────
    public enum KeyState
    {
        None,   // 키 없음 또는 전부 보유
        A,      // 빨강 + 초록
        B,      // 빨강 + 파랑
        C,      // 초록 + 파랑
        D,      // 빨강만
        E,      // 초록만
        F,      // 파랑만
    }

    // ── 이벤트: 상태가 바뀔 때마다 발행 ──────────────────────────────
    /// <summary>
    /// 인자: (이전 상태, 새 상태)
    /// ChoicePresenter 등에서 구독해서 UI를 갱신한다.
    /// </summary>
    public static event Action<KeyState, KeyState> OnStateChanged;

    private static KeyState _current = KeyState.None;
    public static KeyState Current => _current;

    // ── 런타임 초기화 ─────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        _current = KeyState.None;
        // 이벤트 구독자는 씬 리로드 시 자동 해제되므로 null 처리
        OnStateChanged = null;

        // EscapeItemRegistry 변경 이벤트와 연결
        EscapeItemRegistry.OnChanged += HandleRegistryChanged;
    }

    // ── EscapeItemRegistry 변경 콜백 ──────────────────────────────────
    private static void HandleRegistryChanged(int collected, int required)
    {
        Evaluate();
    }

    // ── 핵심: 현재 보유 키로 상태 계산 ──────────────────────────────
    /// <summary>
    /// EscapeItemRegistry 를 보고 현재 KeyState 를 계산한다.
    /// 상태가 바뀌면 OnStateChanged 이벤트를 발행한다.
    /// 외부에서 직접 호출도 가능하다.
    /// </summary>
    public static void Evaluate()
    {
        bool red   = EscapeItemRegistry.Has("redKey");
        bool green = EscapeItemRegistry.Has("greenKey");
        bool blue  = EscapeItemRegistry.Has("blueKey");

        KeyState next = Classify(red, green, blue);

        if (next == _current) return;

        KeyState prev = _current;
        _current = next;

        Debug.Log($"[KeyState] {prev} → {next}  (R:{red} G:{green} B:{blue})");
        OnStateChanged?.Invoke(prev, next);
    }

    // ── 분류 테이블 ───────────────────────────────────────────────────
    private static KeyState Classify(bool red, bool green, bool blue)
    {
        // 3개 전부 보유 → 탈출 조건, 선택지 불필요
        if (red && green && blue) return KeyState.None;

        // 2개 조합
        if (red   && green && !blue) return KeyState.A;
        if (red   && !green && blue) return KeyState.B;
        if (!red  && green && blue)  return KeyState.C;

        // 단일 보유
        if (red   && !green && !blue) return KeyState.D;
        if (!red  && green  && !blue) return KeyState.E;
        if (!red  && !green && blue)  return KeyState.F;

        // 아무것도 없음
        return KeyState.None;
    }
}
