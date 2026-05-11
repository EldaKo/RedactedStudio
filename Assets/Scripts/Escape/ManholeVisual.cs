using System.Collections;
using UnityEngine;

/// <summary>
/// 맨홀 비주얼 컨트롤러.
/// - 열쇠를 다 모으면 Glow 오브젝트 활성화 + 파동 이펙트
/// - 포인트 라이트 깜빡임
/// - 맨홀 테두리 / 구멍 머티리얼 색상 초기화
/// </summary>
public class ManholeVisual : MonoBehaviour
{
    [Header("맨홀 파트")]
    public Renderer rimRenderer;    // Manhole_Rim  (회색 테두리)
    public Renderer darkRenderer;   // Manhole_Dark (검은 구멍)
    public GameObject glowObject;   // Manhole_Glow (파란 글로우, 초기 비활성)

    [Header("포인트 라이트 (선택)")]
    public Light manholeLight;
    public float lightFlickerSpeed = 3f;
    public float lightIntensityMin = 0.4f;
    public float lightIntensityMax = 1.6f;

    [Header("색상")]
    public Color rimColor   = new Color(0.28f, 0.28f, 0.28f); // 철판 회색
    public Color darkColor  = new Color(0.04f, 0.04f, 0.04f); // 거의 검정
    public Color glowColor  = new Color(0.0f,  0.85f, 1.0f);  // 청록 글로우

    private Material rimMat;
    private Material darkMat;
    private Material glowMat;
    private bool isUnlocked = false;

    void Start()
    {
        // 머티리얼 인스턴스 생성 (씬 공유 방지)
        if (rimRenderer)  { rimMat  = new Material(rimRenderer.material);  rimRenderer.material  = rimMat;  rimMat.color  = rimColor; }
        if (darkRenderer) { darkMat = new Material(darkRenderer.material); darkRenderer.material = darkMat; darkMat.color = darkColor; }
        if (glowObject)
        {
            glowMat = new Material(glowObject.GetComponent<Renderer>().material);
            glowObject.GetComponent<Renderer>().material = glowMat;
            glowMat.color = glowColor;
            glowObject.SetActive(false);
        }

        if (manholeLight) manholeLight.enabled = false;

        // 열쇠 수집 이벤트 구독
        EscapeItemRegistry.OnChanged += OnKeyChanged;
    }

    void OnDestroy()
    {
        EscapeItemRegistry.OnChanged -= OnKeyChanged;
    }

    void OnKeyChanged(int collected, int required)
    {
        if (isUnlocked) return;
        if (collected >= required)
        {
            isUnlocked = true;
            StartCoroutine(UnlockSequence());
        }
    }

    IEnumerator UnlockSequence()
    {
        // 1. Glow 서서히 켜짐
        if (glowObject) glowObject.SetActive(true);
        if (manholeLight)
        {
            manholeLight.enabled = true;
            manholeLight.intensity = 0f;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            if (glowMat) glowMat.color = Color.Lerp(Color.black, glowColor, t);
            if (manholeLight) manholeLight.intensity = Mathf.Lerp(0f, lightIntensityMax, t);
            yield return null;
        }

        // 2. 이후 라이트 깜빡임 루프
        StartCoroutine(FlickerLight());
    }

    IEnumerator FlickerLight()
    {
        while (true)
        {
            float target = Random.Range(lightIntensityMin, lightIntensityMax);
            float speed  = Random.Range(lightFlickerSpeed * 0.7f, lightFlickerSpeed * 1.3f);
            float t = 0f;
            float start = manholeLight ? manholeLight.intensity : 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                if (manholeLight) manholeLight.intensity = Mathf.Lerp(start, target, t);
                yield return null;
            }
        }
    }

    // 에디터에서 맨홀 범위 시각화
    void OnDrawGizmos()
    {
        Gizmos.color = isUnlocked ? Color.cyan : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, 1.2f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.05f, 1.2f);
    }
}
