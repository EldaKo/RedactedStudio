using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KeySpawner : MonoBehaviour
{
    [Header("Key Prefabs")]
    public GameObject keyRedPrefab;
    public GameObject keyGreenPrefab;
    public GameObject keyBluePrefab;

    [Header("Spawn Area")]
    public float areaMinX = -50f;
    public float areaMaxX =  30f;
    public float areaMinZ =  -1f;
    public float areaMaxZ =  43f;

    [Header("Settings")]
    public float heightOffset       = 1.2f;
    public float navSampleRadius    = 5f;
    public float minDistBetweenKeys = 12f;
    public float minDistFromSpawn   = 8f;
    public int   maxAttempts        = 200;

    [Header("Height Filter")]
    [Tooltip("이 y값보다 높은 지면은 나무 위/지붕으로 간주 -> 스폰 제외")]
    public float maxGroundY = 2.0f;

    [Tooltip("플레이어 눈높이(m). 스폰 위치 머리 위로 이만큼 뚫려있어야 유효한 위치로 인정")]
    public float playerEyeHeight = 1.8f;

    [Tooltip("머리 위 장애물 감지용 레이어마스크 (기본 전체 레이어)")]
    public LayerMask obstacleMask = ~0;

    private static readonly Vector3 SpawnPos = new Vector3(-44f, 3.56f, 10f);

    IEnumerator Start()
    {
        // NavMesh + NeoFPS 초기화 완료 대기
        yield return new WaitForSeconds(1.5f);
        SpawnKeys();
    }

    void SpawnKeys()
    {
        var prefabs = new[] { keyRedPrefab, keyGreenPrefab, keyBluePrefab };
        var placed  = new List<Vector3>();

        foreach (var prefab in prefabs)
        {
            if (prefab == null) { Debug.LogWarning("[KeySpawner] 프리팹 없음"); continue; }

            Vector3 pos = FindPoint(placed, minDistBetweenKeys);
            if (pos == Vector3.zero)
                pos = FallbackPoint(placed, minDistBetweenKeys);
            if (pos == Vector3.zero)
                pos = FallbackPoint(placed, minDistBetweenKeys * 0.5f);

            if (pos == Vector3.zero) { Debug.LogWarning($"[KeySpawner] {prefab.name} 배치 실패"); continue; }

            placed.Add(pos);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            go.name = prefab.name;
            Debug.Log($"[KeySpawner] {prefab.name} -> {pos}");
        }
    }

    Vector3 FindPoint(List<Vector3> placed, float minDistBetween)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float rx = Random.Range(areaMinX, areaMaxX);
            float rz = Random.Range(areaMinZ, areaMaxZ);

            if (!NavMesh.SamplePosition(new Vector3(rx, 50f, rz), out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
                continue;

            if (hit.position.y > maxGroundY) continue;

            Vector3 pos = hit.position + Vector3.up * heightOffset;

            if (Physics.Raycast(pos, Vector3.up, playerEyeHeight, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

            if (!Valid(pos, placed, minDistBetween)) continue;
            return pos;
        }
        return Vector3.zero;
    }

    Vector3 FallbackPoint(List<Vector3> placed, float minDistBetween)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float rx = Random.Range(areaMinX, areaMaxX);
            float rz = Random.Range(areaMinZ, areaMaxZ);
            Ray ray = new Ray(new Vector3(rx, 100f, rz), Vector3.down);

            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

            if (hit.point.y > maxGroundY) continue;

            Vector3 pos = hit.point + Vector3.up * heightOffset;

            if (Physics.Raycast(pos, Vector3.up, playerEyeHeight, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

            if (!Valid(pos, placed, minDistBetween)) continue;
            return pos;
        }
        return Vector3.zero;
    }

    bool Valid(Vector3 pos, List<Vector3> placed, float minDistBetween)
    {
        if (Vector3.Distance(pos, SpawnPos) < minDistFromSpawn) return false;
        foreach (var p in placed)
            if (Vector3.Distance(pos, p) < minDistBetween) return false;
        return true;
    }
}
