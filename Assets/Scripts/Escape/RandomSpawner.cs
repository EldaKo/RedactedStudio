using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 맵의 NavMesh/바닥 위 랜덤 위치에 프리팹들을 스폰하는 범용 스포너.
/// 적, 필드 드랍 아이템 등에 공용으로 사용.
/// </summary>
public class RandomSpawner : MonoBehaviour
{
    [Header("Spawn Targets")]
    [Tooltip("스폰 후보 프리팹들")]
    public GameObject[] prefabs;
    [Tooltip("총 스폰 개수")]
    public int spawnCount = 5;

    [Header("Spawn Area (world XZ)")]
    public float areaMinX = -85f;
    public float areaMaxX = 90f;
    public float areaMinZ = -60f;
    public float areaMaxZ = 110f;

    [Header("Placement")]
    [Tooltip("바닥에서 띄울 높이")]
    public float heightOffset = 0.5f;
    [Tooltip("스폰된 오브젝트 간 최소 거리")]
    public float minDistBetween = 5f;
    [Tooltip("플레이어 스폰 지점에서 최소 거리")]
    public float minDistFromPlayer = 10f;
    public Vector3 playerSpawn = new Vector3(-44f, 3.56f, 10f);
    [Tooltip("유효 바닥 높이 범위 (지붕/지하 제외)")]
    public float maxGroundY = 4f;
    public float minGroundY = -2f;
    public int maxAttempts = 500;
    public LayerMask groundMask = ~0;

    [Header("NavMesh (적처럼 이동 가능 위치가 필요할 때)")]
    [Tooltip("true면 NavMesh 위 위치만 허용 (적 권장). false면 바닥 raycast만 (아이템 권장)")]
    public bool requireNavMesh = false;
    public float navSampleRadius = 3f;

    [Header("Timing")]
    [Tooltip("씬 시작 후 스폰까지 대기 (NavMesh/플레이어 초기화 대기)")]
    public float startDelay = 1.5f;

    IEnumerator Start()
    {
        // Realtime 사용: timeScale=0(일시정지 등)에도 영향받지 않음
        yield return new WaitForSecondsRealtime(startDelay);
        Spawn();
    }

    void Spawn()
    {
        Debug.Log($"[RandomSpawner:{name}] Spawn 시작 (count={spawnCount}, navMesh={requireNavMesh}, prefabs={(prefabs == null ? 0 : prefabs.Length)})");

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning($"[RandomSpawner:{name}] 프리팹 없음");
            return;
        }

        var placed = new List<Vector3>();
        int success = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) continue;

            Vector3 pos = FindPoint(placed);
            if (pos == Vector3.zero)
            {
                Debug.LogWarning($"[RandomSpawner:{name}] {prefab.name} 배치 실패 ({i + 1}/{spawnCount})");
                continue;
            }

            placed.Add(pos);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.name = prefab.name;
            success++;
            Debug.Log($"[RandomSpawner:{name}] {prefab.name} -> {pos}");
        }

        Debug.Log($"[RandomSpawner:{name}] 완료: {success}/{spawnCount} 스폰됨");
    }

    Vector3 FindPoint(List<Vector3> placed)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float rx = Random.Range(areaMinX, areaMaxX);
            float rz = Random.Range(areaMinZ, areaMaxZ);

            // 먼저 raycast로 바닥 지점을 찾는다 (아이템/적 공통)
            Ray ray = new Ray(new Vector3(rx, 200f, rz), Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, 400f, groundMask, QueryTriggerInteraction.Ignore))
                continue;
            if (hit.point.y > maxGroundY || hit.point.y < minGroundY) continue;

            Vector3 groundPoint = hit.point;

            // 적처럼 NavMesh가 필요하면, 바닥 지점 근처의 NavMesh로 스냅
            if (requireNavMesh)
            {
                if (!NavMesh.SamplePosition(groundPoint, out NavMeshHit nav, navSampleRadius, NavMesh.AllAreas))
                    continue;
                groundPoint = nav.position;
            }

            Vector3 pos = groundPoint + Vector3.up * heightOffset;

            if (!Valid(pos, placed)) continue;
            return pos;
        }
        return Vector3.zero;
    }

    bool Valid(Vector3 pos, List<Vector3> placed)
    {
        if (Vector3.Distance(pos, playerSpawn) < minDistFromPlayer) return false;
        foreach (var p in placed)
            if (Vector3.Distance(pos, p) < minDistBetween) return false;
        return true;
    }
}
