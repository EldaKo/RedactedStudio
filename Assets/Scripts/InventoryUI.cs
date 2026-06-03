using UnityEngine;
using NeoFPS;

public class InventoryUI : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Tab으로 켜고 끌 인벤토리 패널")]
    public GameObject panelRoot;

    [Header("Slots")]
    [Tooltip("각 슬롯은 InventorySlotUI 컴포넌트를 가진 GameObject. 인벤토리 slotCount와 길이가 일치해야 함")]
    public InventorySlotUI[] slots;

    [Header("Helpers")]
    public ItemContextMenu contextMenu;
    public ItemTooltip tooltip;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("While Open")]
    [Tooltip("인벤토리 열려 있는 동안 비활성화할 컴포넌트(예: MouseLook, PlayerMovement)")]
    public Behaviour[] disableWhileOpen;
    [Tooltip("열릴 때 커서 해제, 닫힐 때 잠금")]
    public bool manageCursor = true;
    [Tooltip("열려 있는 동안 Time.timeScale = 0")]
    public bool pauseTime = true;
    [Tooltip("NeoFPS 커서/입력 시스템과 연동")]
    public bool useNeoFpsInput = true;

    private float prevTimeScale = 1f;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        AutoWireSlotsIfNeeded();

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) slots[i].Bind(this, i);
            }
        }

        if (contextMenu != null) contextMenu.Bind(this);
        if (tooltip != null) tooltip.Hide();

        if (Inventory.HasInstance)
        {
            Inventory.Instance.OnInventoryChanged += Refresh;
            Refresh();
        }

        ApplyCursor(false);
        SetWhileOpenComponents(true);
    }

    private void OnDestroy()
    {
        if (Inventory.HasInstance)
        {
            Inventory.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);
        Refresh();
        ApplyCursor(true);
        SetWhileOpenComponents(false);
        ApplyTimePause(true);
    }

    public void Close()
    {
        if (panelRoot == null) return;
        if (contextMenu != null) contextMenu.Close();
        if (tooltip != null) tooltip.Hide();
        panelRoot.SetActive(false);
        ApplyCursor(false);
        SetWhileOpenComponents(true);
        ApplyTimePause(false);
    }

    private void ApplyTimePause(bool open)
    {
        if (!pauseTime) return;
        if (open)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
        }
    }

    private void Refresh()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            ItemStack stack = Inventory.HasInstance ? Inventory.Instance.GetSlot(i) : null;
            slots[i].Refresh(stack);
        }
    }

    public void OnSlotRightClicked(InventorySlotUI slot, Vector2 screenPos)
    {
        if (contextMenu != null && slot != null)
        {
            if (tooltip != null) tooltip.Hide();
            contextMenu.Open(slot.slotIndex, screenPos);
        }
    }

    public void OnSlotHoverEnter(InventorySlotUI slot)
    {
        if (tooltip == null || slot == null || slot.CurrentStack == null) return;
        if (contextMenu != null && contextMenu.IsOpen) return;
        tooltip.Show(slot.CurrentStack.data);
    }

    public void OnSlotHoverExit(InventorySlotUI slot)
    {
        if (tooltip != null) tooltip.Hide();
    }

    private void AutoWireSlotsIfNeeded()
    {
        bool needsAutoWire = slots == null || slots.Length == 0;
        if (!needsAutoWire && slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) { needsAutoWire = true; break; }
            }
        }
        if (!needsAutoWire) return;

        Transform root = panelRoot != null ? panelRoot.transform : transform;
        var found = root.GetComponentsInChildren<InventorySlotUI>(true);
        if (found != null && found.Length > 0) slots = found;
    }

    private void ApplyCursor(bool open)
    {
        if (!manageCursor) return;
        if (useNeoFpsInput)
        {
            NeoFpsInputManagerBase.captureMouseCursor = !open;
        }
        else
        {
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }
    }

    private void SetWhileOpenComponents(bool enabled)
    {
        if (disableWhileOpen == null) return;
        foreach (var b in disableWhileOpen)
        {
            if (b != null) b.enabled = enabled;
        }
    }
}
