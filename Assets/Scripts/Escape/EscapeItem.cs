using UnityEngine;

/// <summary>
/// 탈출 아이템. 플레이어가 트리거에 닿으면 자동으로 수집된다.
/// 수집 후 자체 파괴.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EscapeItem : MonoBehaviour
{
    [Tooltip("아이템 고유 ID. 같은 ID는 한 번만 카운트된다.")]
    public string itemId = "item_default";

    [Tooltip("수집되면 호출되는 효과 (옵션)")]
    public AudioClip pickupSound;

    [Header("Visual")]
    public float bobAmplitude = 0.08f;   // 위아래로 떠다니는 진폭
    public float bobFrequency = 1.5f;    // 진동 속도
    public float rotateSpeed = 60f;      // 회전 속도 (deg/s)

    private Vector3 baseLocalPos;
    private Transform mesh;

    private void Awake()
    {
        // 자식 메쉬가 있으면 그걸 회전·부유시킨다
        if (transform.childCount > 0)
        {
            mesh = transform.GetChild(0);
            baseLocalPos = mesh.localPosition;
        }

        // 트리거 콜라이더 보장
        var col = GetComponent<Collider>();
        col.isTrigger = true;
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
        Pickup(other.transform.position);
    }

    private void Pickup(Vector3 position)
    {
        EscapeItemRegistry.Collect(itemId);

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, position, 0.7f);
        }

        Destroy(gameObject);
    }
}
