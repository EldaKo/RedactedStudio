using UnityEngine;

/// <summary>
/// 씬에 배치하는 열쇠 픽업 오브젝트.
/// 플레이어가 콜라이더에 닿으면 EscapeItemRegistry에 등록되고 사라진다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour
{
    [Header("Key Settings")]
    [Tooltip("redKey / greenKey / blueKey 등 고유 ID")]
    public string keyId = "redKey";

    [Tooltip("HUD·알림에 표시할 이름 (예: 빨간 열쇠)")]
    public string displayName = "빨간 열쇠";

    [Header("Visual")]
    public float bobAmplitude  = 0.12f;
    public float bobFrequency  = 1.2f;
    public float rotateSpeed   = 80f;

    [Header("Audio")]
    public AudioClip pickupSound;

    // ── 내부 ──────────────────────────────────────────
    private Vector3 baseLocalPos;
    private Transform mesh;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (transform.childCount > 0)
        {
            mesh = transform.GetChild(0);
            baseLocalPos = mesh.localPosition;
        }
    }

    private void Update()
    {
        if (mesh == null) return;
        float y = baseLocalPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        mesh.localPosition = new Vector3(baseLocalPos.x, y, baseLocalPos.z);
        mesh.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        bool added = EscapeItemRegistry.Collect(keyId);
        if (!added) return; // 이미 가지고 있음

        // 픽업 알림 이벤트 발행
        EscapeEvents.NotifyKeyPickup(displayName,
            EscapeItemRegistry.CollectedCount,
            EscapeItemRegistry.RequiredCount);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = KeyColor();
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }

    private Color KeyColor()
    {
        switch (keyId)
        {
            case "redKey":   return Color.red;
            case "greenKey": return Color.green;
            case "blueKey":  return Color.blue;
            default:         return Color.yellow;
        }
    }
#endif
}
