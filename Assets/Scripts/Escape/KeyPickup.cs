using UnityEngine;

// [추가] 이 스크립트를 넣으면 유니티가 알아서 Collider를 붙여줍니다.
[RequireComponent(typeof(Collider))] 
public class KeyPickup : MonoBehaviour
{
    [Header("열쇠 정보")]
    public string keyId = "blueKey";       
    public string displayName = "파란 열쇠"; 

    [Header("애니메이션 효과")]
    public float bobAmplitude = 0.12f;     
    public float bobFrequency = 1.2f;      
    public float rotateSpeed = 80f;        

    [Header("효과음")]
    public AudioClip pickupSound;          

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        // ==========================================
        // [해결 1] 강제 트리거 설정 (튕김 방지)
        // 시작하자마자 코드가 강제로 IsTrigger를 체크해버립니다.
        // 이제 캐릭터랑 단단하게 부딪히지 않고 스르륵 먹어집니다!
        // ==========================================
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
        float newY = startPos.y + Mathf.Sin(Time.time * Mathf.PI * 2f * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectKey();
        }
    }

    public void CollectKey()
    {
        // ==========================================
        // [해결 2] 레지스트리에 수집 알림 (문 열림 버그 수정)
        // ItemHideout에 직접 넣지 말고, EscapeItemRegistry를 통해 수집합니다.
        // 보내주신 Registry 코드를 보니, 이렇게 하면 알아서 은신처에도 저장되고 
        // 탈출구 HUD 숫자도 정상적으로 올라갑니다!
        // ==========================================
        bool isCollected = EscapeItemRegistry.Collect(keyId);

        // 정상적으로 수집이 처리되었다면 사운드 재생 후 파괴
        if (isCollected)
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            Destroy(gameObject);
        }
    }
}