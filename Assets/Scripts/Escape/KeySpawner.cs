using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeySpawner : MonoBehaviour
{
    [Header("Key Prefabs")]
    public GameObject keyRedPrefab;
    public GameObject keyGreenPrefab;
    public GameObject keyBluePrefab;

    [Header("Spawn Area")]
    public float areaMinX = -85f;
    public float areaMaxX =  90f;
    public float areaMinZ = -60f;
    public float areaMaxZ = 110f;

    [Header("Settings")]
    public float heightOffset       = 0.5f;
    public float minDistBetweenKeys = 40f;
    public float minDistFromSpawn   = 15f;
    public int   maxAttempts        = 500;

    [Header("Ground Raycast")]
    public LayerMask groundMask = ~0;

    private static readonly Vector3 PlayerSpawn = new Vector3(-44f, 3.56f, 10f);

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1.5f);
        SpawnKeys();
    }

    void SpawnKeys()
    {
        var prefabs = new[] { keyRedPrefab, keyGreenPrefab, keyBluePrefab };
        var placed  = new List<Vector3>();

        foreach (var prefab in prefabs)
        {
            if (prefab == null) { Debug.LogWarning("[KeySpawner] 프리팩 없음"); continue; }

            Vector3 pos = FindPoint(placed);

            if (pos == Vector3.zero) { Debug.LogWarning($"[KeySpawner] {prefab.name} 배치 실패"); continue; }

            placed.Add(pos);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            go.name = prefab.name;
            Debug.Log($"[KeySpawner] {prefab.name} -> {pos}");
        }
    }

    Vector3 FindPoint(List<Vector3> placed)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            float rx = Random.Range(areaMinX, areaMaxX);
            float rz = Random.Range(areaMinZ, areaMaxZ);

            // 위에서 아래로 Raycast로 바닥 찾기
            Ray ray = new Ray(new Vector3(rx, 200f, rz), Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, 400f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // 너무 높은 데 제외 (지붕, 나무 위 등)
            if (hit.point.y > 4f || hit.point.y < -2f) continue;

            Vector3 pos = hit.point + Vector3.up * heightOffset;

            if (!Valid(pos, placed)) continue;
            return pos;
        }
        return Vector3.zero;
    }

    bool Valid(Vector3 pos, List<Vector3> placed)
    {
        if (Vector3.Distance(pos, PlayerSpawn) < minDistFromSpawn) return false;
        foreach (var p in placed)
            if (Vector3.Distance(pos, p) < minDistBetweenKeys) return false;
        return true;
    }
}