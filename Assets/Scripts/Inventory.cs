using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("Settings")]
    public int slotCount = 3;

    private ItemStack[] slots;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        slots = new ItemStack[slotCount];
    }

    public bool TryAdd(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // 1단계: 기존 스택에 추가
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].data == item)
            {
                int canAdd = item.maxStack - slots[i].count;
                int adding = Mathf.Min(canAdd, amount);
                slots[i].count += adding;
                amount -= adding;

                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 2단계: 빈 슬롯에 새 스택 생성
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                int adding = Mathf.Min(item.maxStack, amount);
                slots[i] = new ItemStack { data = item, count = adding };
                amount -= adding;

                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 3단계: 남은 양이 있으면 꽉 찬 것
        OnInventoryChanged?.Invoke(); // 일부라도 들어갔을 수 있으니 갱신
        return false;
    }

    // UI가 슬롯 정보를 읽어갈 때 쓰는 함수
    public ItemStack GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public int SlotCount => slots.Length;
}

// ItemStack 클래스 (Inventory.cs 안에 같이 둬도 됨)
[Serializable]
public class ItemStack
{
    public ItemData data;
    public int count;
}