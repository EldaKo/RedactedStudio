using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public Image icon;
    public TextMeshProUGUI countText;

    [HideInInspector] public int slotIndex;

    private InventoryUI owner;
    private ItemStack currentStack;

    public ItemStack CurrentStack => currentStack;

    public void Bind(InventoryUI ui, int index)
    {
        owner = ui;
        slotIndex = index;
    }

    public void Refresh(ItemStack stack)
    {
        currentStack = stack;

        if (stack == null || stack.data == null)
        {
            if (icon != null) icon.gameObject.SetActive(false);
            if (countText != null) countText.gameObject.SetActive(false);
            return;
        }

        if (icon != null)
        {
            icon.sprite = stack.data.icon;
            icon.gameObject.SetActive(true);
        }

        if (countText != null)
        {
            if (stack.count > 1)
            {
                countText.text = stack.count.ToString();
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentStack == null || currentStack.data == null) return;
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            owner?.OnSlotRightClicked(this, eventData.position);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentStack == null || currentStack.data == null) return;
        owner?.OnSlotHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.OnSlotHoverExit(this);
    }
}
