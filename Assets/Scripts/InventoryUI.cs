using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Slot References")]
    public Image[] iconImages;     
    public TextMeshProUGUI[] countTexts;  

    private void Start()
    {
        // 이벤트 구독
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += Refresh;
            Refresh(); // 시작할 때 한 번 갱신 (빈 상태 반영)
        }
    }

    private void OnDestroy()
    {
        // 구독 해제 (메모리 누수 방지)
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < iconImages.Length; i++)
        {
            ItemStack stack = Inventory.Instance.GetSlot(i);

            if (stack == null || stack.data == null)
            {
                // 빈 슬롯
                iconImages[i].gameObject.SetActive(false);
                countTexts[i].gameObject.SetActive(false);
            }
            else
            {
                // 아이템 있음
                iconImages[i].sprite = stack.data.icon;
                iconImages[i].gameObject.SetActive(true);

                // 개수가 2개 이상일 때만 텍스트 표시
                if (stack.count > 1)
                {
                    countTexts[i].text = stack.count.ToString();
                    countTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    countTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }
}