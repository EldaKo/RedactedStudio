using UnityEngine;
using System.Collections.Generic;

public class ItemHideout : MonoBehaviour
{
    public static ItemHideout Instance;
    public List<string> collectedKeys = new List<string>();

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void AddKey(string keyId)
    {
        if (!collectedKeys.Contains(keyId)) collectedKeys.Add(keyId);
    }

    public bool HasKey(string keyId)
    {
        return collectedKeys.Contains(keyId);
    }

    // [추가] 열쇠를 사용(소모)하는 함수
    public void RemoveKey(string keyId)
    {
        if (collectedKeys.Contains(keyId))
        {
            collectedKeys.Remove(keyId);
            Debug.Log($"[ItemHideout] 열쇠 소모 완료: {keyId}");
        }
    }
}