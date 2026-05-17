using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    public float promptCooldown = 2.5f;

    [Header("열쇠 근접 알림")]
    [Tooltip("이 거리 이내에 열쇠가 있으면 근처 알림을 표시합니다")]
    public float keyNearbyDistance = 8f;

    [Header("클리어 후 씬 전환")]
    [Tooltip("탈출 성공 시 이동할 씬 이름 (Build Settings에 등록되어 있어야 함)")]
    public string nextSceneName = "Hideout";

    [Tooltip("클리어 화면을 보여준 뒤 다음 씬으로 넘어가기까지 대기 시간 (unscaled)")]
    public float clearScreenDuration = 2.5f;

    private bool cleared;
    private float nextPromptTime;

    // IMGUI 상태
    private bool showFail;
    private float failUntil;
    private bool showClear;
    private bool showKeyNearby;

    private GUIStyle bigStyle;
    private GUIStyle hudStyle;
    private GUIStyle boxStyle;
    private GUIStyle btnStyle;
    private GUIStyle nearbyStyle;

    private Transform playerTransform;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform != null && !EscapeItemRegistry.HasAll())
            showKeyNearby = IsKeyNearby();
        else
            showKeyNearby = false;
    }

    bool IsKeyNearby()
    {
        string[] keyNames = { "Key_Red", "Key_Green", "Key_Blue" };
        foreach (var keyName in keyNames)
        {
            var keyObj = GameObject.Find(keyName);
            if (keyObj != null && keyObj.activeInHierarchy)
            {
                if (Vector3.Distance(playerTransform.position, keyObj.transform.position) <= keyNearbyDistance)
                    return true;
            }
        }

        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            if (obj.activeInHierarchy && obj.name.StartsWith("Key") && !obj.name.Contains("Spawner"))
            {
                if (Vector3.Distance(playerTransform.position, obj.transform.position) <= keyNearbyDistance)
                    return true;
            }
        }

        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (cleared || !other.CompareTag("Player")) return;

        if (EscapeItemRegistry.HasAll())
        {
            cleared = true;
            showClear = true;
            EscapeEvents.NotifyEscapeSuccess();
            StartCoroutine(HandleEscapeClear());
        }
        else
        {
            if (Time.time < nextPromptTime) return;
            nextPromptTime = Time.time + promptCooldown;
            showFail = true;
            failUntil = Time.unscaledTime + 3f;
        }
    }

    IEnumerator HandleEscapeClear()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(clearScreenDuration);
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    void EnsureStyles()
    {
        if (hudStyle != null) return;

        hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        hudStyle.normal.textColor = Color.white;

        bigStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 52,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        bigStyle.normal.textColor = Color.white;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter
        };
        boxStyle.normal.textColor = Color.white;

        btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };

        nearbyStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        nearbyStyle.normal.textColor = new Color(1f, 0.95f, 0.4f);
    }

    void OnGUI()
    {
        EnsureStyles();

        int got  = EscapeItemRegistry.CollectedCount;
        int need = EscapeItemRegistry.RequiredCount;

        // ── 상단 HUD
        string hudText = EscapeItemRegistry.HasAll()
            ? $"열쇠 {got}/{need}  ▶  출구로 가세요!"
            : $"열쇠 {got}/{need}";
        GUI.Label(new Rect(20, 20, 400, 36), hudText, hudStyle);

        // ── 탈출 불가 팝업
        const float failBoxX = 20f;
        const float failBoxY = 62f;
        const float failBoxW = 260f;
        const float failBoxH = 64f;

        if (showFail && Time.unscaledTime < failUntil)
        {
            float remaining = failUntil - Time.unscaledTime;
            float alpha = Mathf.Clamp01(remaining / 0.6f);

            var prevColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.35f * alpha);
            GUI.DrawTexture(new Rect(failBoxX, failBoxY, failBoxW, failBoxH), Texture2D.whiteTexture);
            GUI.color = prevColor;

            int missing = need - got;
            var fadeBox = new GUIStyle(boxStyle) { fontSize = 16 };
            fadeBox.normal.textColor = new Color(1f, 1f, 1f, alpha);

            GUI.Box(new Rect(failBoxX, failBoxY, failBoxW, failBoxH),
                $"열쇠가 {missing}개 부족합니다 ({got}/{need})",
                fadeBox);
        }
        else
        {
            showFail = false;
        }

        // ── 열쇠 근처 알림 (실패 팝업 아래)
        if (showKeyNearby)
        {
            float nearX = failBoxX;
            float nearY = failBoxY + failBoxH + 4f;
            float nearW = failBoxW;
            float nearH = 40f;

            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(nearX, nearY, nearW, nearH), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUI.Box(new Rect(nearX, nearY, nearW, nearH), "열쇠가 근처에 있어.", nearbyStyle);
        }

        // ── 클리어 화면
        if (showClear)
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 80),
                "ESCAPE  CLEAR!", bigStyle);

            var subStyle = new GUIStyle(bigStyle) { fontSize = 22 };
            GUI.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 40),
                "열쇠 3개를 모두 모아 탈출에 성공했습니다!", subStyle);
        }
    }
}

