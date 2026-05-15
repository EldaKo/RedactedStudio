using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 EscapeItem 근처에 있는지 매 프레임 체크하고,
/// 근처에 있는 아이템 ID 리스트를 제공한다.
/// ExitZone의 HUD가 이 정보를 읽어 알림을 표시한다.
/// </summary>
public class EscapeProximityTracker : MonoBehaviour
{
    [Tooltip("이 거리 안에 있으면 '근처' 로 간주")]
    public float proximityRadius = 15f;

    [Tooltip("체크 주기(초). 0이면 매 프레임")]
    public float checkInterval = 0.2f;

    public static EscapeProximityTracker Instance { get; private set; }

    private Transform playerTransform;
    private float nextCheckTime;
    private readonly List<EscapeItem> nearbyItems = new List<EscapeItem>();

    /// <summary>현재 근처에 있는 아이템들 (읽기 전용 리스트).</summary>
    public IReadOnlyList<EscapeItem> NearbyItems => nearbyItems;

    /// <summary>가장 가까운 아이템 (없으면 null).</summary>
    public EscapeItem ClosestItem { get; private set; }
    public float ClosestDistance { get; private set; } = float.MaxValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) playerTransform = playerGo.transform;
    }

    private void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        if (playerTransform == null)
        {
            // 플레이어가 아직 로드 안 됐을 수 있음 — 다시 시도
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) playerTransform = playerGo.transform;
            else return;
        }

        nearbyItems.Clear();
        ClosestItem = null;
        ClosestDistance = float.MaxValue;

        // 씬의 모든 EscapeItem 탐색 (개수가 적어 부담 적음)
        var items = Object.FindObjectsByType<EscapeItem>(FindObjectsSortMode.None);
        Vector3 playerPos = playerTransform.position;
        float radiusSqr = proximityRadius * proximityRadius;

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == null) continue;
            float distSqr = (item.transform.position - playerPos).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                nearbyItems.Add(item);
                float dist = Mathf.Sqrt(distSqr);
                if (dist < ClosestDistance)
                {
                    ClosestDistance = dist;
                    ClosestItem = item;
                }
            }
        }
    }
}
