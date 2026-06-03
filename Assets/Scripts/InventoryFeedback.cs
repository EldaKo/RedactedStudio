using UnityEngine;
using TMPro;

public class InventoryFeedback : MonoBehaviour
{
    public static InventoryFeedback Instance { get; private set; }

    [Header("References")]
    public CanvasGroup group;
    public TextMeshProUGUI text;

    [Header("Timing")]
    public float showSeconds = 1.2f;
    public float fadeSeconds = 0.4f;

    private float remaining;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (group != null) group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Notify(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (Instance == null)
        {
            Debug.Log($"[Inventory] {message}");
            return;
        }
        Instance.Show(message);
    }

    public void Show(string message)
    {
        if (text != null) text.text = message;
        if (group != null) group.alpha = 1f;
        remaining = showSeconds + fadeSeconds;
    }

    private void Update()
    {
        if (group == null || remaining <= 0f) return;
        remaining -= Time.unscaledDeltaTime;
        if (remaining < fadeSeconds)
        {
            group.alpha = Mathf.Clamp01(remaining / fadeSeconds);
        }
        if (remaining <= 0f) group.alpha = 0f;
    }
}
