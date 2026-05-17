using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemContextMenu : MonoBehaviour
{
    [Header("Root")]
    public GameObject panel;
    public RectTransform panelRect;

    [Header("Use")]
    public Button useButton;
    public TextMeshProUGUI useLabel;

    [Header("Drop")]
    public Button dropOneButton;
    public Button dropAllButton;
    public TextMeshProUGUI dropAllLabel;
    [Tooltip("전부 드롭 라벨 포맷. {0} 자리에 수량이 들어감")]
    public string dropAllFormat = "Drop all ({0})";

    [Header("Feedback Messages")]
    public string msgCantUse = "Cannot use";
    public string msgDroppedOne = "Dropped 1";
    public string msgDroppedFormat = "Dropped {0}";
    public string msgCantDrop = "Cannot drop";

    private InventoryUI owner;
    private int slotIndex = -1;
    private bool justOpened;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);

        if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
        if (dropOneButton != null) dropOneButton.onClick.AddListener(OnDropOneClicked);
        if (dropAllButton != null) dropAllButton.onClick.AddListener(OnDropAllClicked);
    }

    public void Bind(InventoryUI ui) => owner = ui;

    public void Open(int slotIndex, Vector2 screenPos)
    {
        if (panel == null) return;
        this.slotIndex = slotIndex;

        var stack = Inventory.HasInstance ? Inventory.Instance.GetSlot(slotIndex) : null;
        if (stack == null || stack.data == null) { Close(); return; }

        var data = stack.data;
        GameObject user = Inventory.HasInstance ? Inventory.Instance.ResolveUseTarget() : null;

        bool canUse = data.useAction != null && data.useAction.CanUse(user);
        if (useButton != null)
        {
            useButton.gameObject.SetActive(data.useAction != null);
            useButton.interactable = canUse;
        }

        bool canDrop = data.canDrop && data.worldPrefab != null;
        if (dropOneButton != null) dropOneButton.gameObject.SetActive(canDrop);
        if (dropAllButton != null)
        {
            bool showAll = canDrop && stack.count > 1;
            dropAllButton.gameObject.SetActive(showAll);
            if (dropAllLabel != null) dropAllLabel.text = string.Format(dropAllFormat, stack.count);
        }

        panel.SetActive(true);
        if (panelRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            panelRect.position = ClampToScreen(screenPos);
        }
        justOpened = true;
    }

    public void Close()
    {
        slotIndex = -1;
        if (panel != null) panel.SetActive(false);
    }

    private void LateUpdate()
    {
        justOpened = false;
    }

    private void Update()
    {
        if (!IsOpen || justOpened) return;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (panelRect == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
            {
                Close();
            }
        }
    }

    private void OnUseClicked()
    {
        if (slotIndex < 0 || !Inventory.HasInstance) { Close(); return; }
        var stack = Inventory.Instance.GetSlot(slotIndex);
        if (stack == null || stack.data == null) { Close(); return; }

        var user = Inventory.Instance.ResolveUseTarget();
        var action = stack.data.useAction;

        if (action == null || !action.CanUse(user))
        {
            InventoryFeedback.Notify(action != null ? action.GetFailMessage(user) : msgCantUse);
            Close();
            return;
        }

        bool ok = Inventory.Instance.TryUse(slotIndex);
        InventoryFeedback.Notify(ok ? action.GetSuccessMessage(user) : action.GetFailMessage(user));
        Close();
    }

    private void OnDropOneClicked()
    {
        if (slotIndex < 0 || !Inventory.HasInstance) { Close(); return; }
        bool ok = Inventory.Instance.TryDrop(slotIndex, 1);
        InventoryFeedback.Notify(ok ? msgDroppedOne : msgCantDrop);
        Close();
    }

    private void OnDropAllClicked()
    {
        if (slotIndex < 0 || !Inventory.HasInstance) { Close(); return; }
        var stack = Inventory.Instance.GetSlot(slotIndex);
        int n = stack?.count ?? 0;
        bool ok = Inventory.Instance.TryDrop(slotIndex, n);
        InventoryFeedback.Notify(ok ? string.Format(msgDroppedFormat, n) : msgCantDrop);
        Close();
    }

    private Vector2 ClampToScreen(Vector2 pos)
    {
        if (panelRect == null) return pos;
        Vector2 size = panelRect.rect.size * panelRect.lossyScale;
        pos.x = Mathf.Clamp(pos.x, 0f, Screen.width - size.x);
        pos.y = Mathf.Clamp(pos.y, size.y, Screen.height);
        return pos;
    }
}
