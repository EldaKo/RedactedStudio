using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    [Header("References")]
    public RectTransform panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    [Header("Behavior")]
    [Tooltip("켜면 마우스 따라다님. 끄면 Inspector에 배치한 자리 그대로.")]
    public bool followCursor = false;

    [Header("Cursor Mode")]
    [Tooltip("마우스 포인터로부터의 오프셋 (followCursor 켤 때만 사용)")]
    public Vector2 cursorOffset = new Vector2(16f, -16f);
    [Tooltip("화면 안에 들어오게 보정")]
    public bool clampToScreen = true;

    private RectTransform parentCanvas;

    private void Awake()
    {
        if (panel != null) panel.gameObject.SetActive(false);
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) parentCanvas = canvas.transform as RectTransform;
    }

    private void Update()
    {
        if (!followCursor) return;
        if (panel != null && panel.gameObject.activeSelf)
        {
            UpdateCursorPosition();
        }
    }

    public void Show(ItemData data)
    {
        if (panel == null || data == null) return;
        if (nameText != null) nameText.text = data.itemName;
        if (descriptionText != null)
        {
            descriptionText.text = data.description;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(data.description));
        }
        panel.gameObject.SetActive(true);
        if (followCursor) UpdateCursorPosition();
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    private void UpdateCursorPosition()
    {
        Vector2 pos = (Vector2)Input.mousePosition + cursorOffset;

        if (clampToScreen && parentCanvas != null)
        {
            Vector2 size = panel.rect.size * parentCanvas.lossyScale;
            pos.x = Mathf.Clamp(pos.x, 0f, Screen.width - size.x);
            pos.y = Mathf.Clamp(pos.y, size.y, Screen.height);
        }

        panel.position = pos;
    }
}
