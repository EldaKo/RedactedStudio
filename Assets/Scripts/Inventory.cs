using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("Settings")]
    public int slotCount = 3;

    [Header("Drop")]
    [Tooltip("드롭 시 아이템을 스폰할 기준(보통 플레이어 카메라). 비우면 이 오브젝트의 transform 사용.")]
    public Transform dropOrigin;
    [Tooltip("dropOrigin 앞쪽으로 얼마나 떨어뜨려 스폰할지")]
    public float dropForwardDistance = 1.2f;
    [Tooltip("드롭 시 약간 위로 띄울 거리")]
    public float dropUpOffset = 0.2f;
    [Tooltip("드롭 시 앞쪽으로 던지는 힘 (Rigidbody가 있을 때)")]
    public float dropForwardForce = 2f;

    [Header("Use Target")]
    [Tooltip("ItemUseAction에 user로 넘길 오브젝트(보통 플레이어 루트). 비우면 이 오브젝트.")]
    public GameObject useTarget;

    private ItemStack[] slots;

    public event Action OnInventoryChanged;

    public int SlotCount => slots?.Length ?? slotCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        slots = new ItemStack[slotCount];
    }

    public int GetTotalCount(ItemData item)
    {
        if (item == null || slots == null) return 0;
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].data == item) total += slots[i].count;
        }
        return total;
    }

    public bool TryConsume(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || slots == null) return false;
        if (GetTotalCount(item) < amount) return false;

        int remaining = amount;
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i] == null || slots[i].data != item) continue;
            int take = Mathf.Min(slots[i].count, remaining);
            slots[i].count -= take;
            remaining -= take;
            if (slots[i].count <= 0) slots[i] = null;
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public ItemStack GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public void ClearAll()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++) slots[i] = null;
        OnInventoryChanged?.Invoke();
    }

    public void SetSlot(int index, ItemData data, int count)
    {
        if (slots == null || index < 0 || index >= slots.Length || data == null || count <= 0) return;
        slots[index] = new ItemStack { data = data, count = count };
        OnInventoryChanged?.Invoke();
    }

    public bool TryAdd(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || slots == null) return false;

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i] != null && slots[i].data == item)
            {
                int canAdd = item.maxStack - slots[i].count;
                int adding = Mathf.Min(canAdd, amount);
                if (adding > 0)
                {
                    slots[i].count += adding;
                    amount -= adding;
                }
            }
        }

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            if (slots[i] == null)
            {
                int adding = Mathf.Min(item.maxStack, amount);
                slots[i] = new ItemStack { data = item, count = adding };
                amount -= adding;
            }
        }

        OnInventoryChanged?.Invoke();
        return amount <= 0;
    }

    public int Remove(int slotIndex, int count = -1)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return 0;
        var stack = slots[slotIndex];
        if (stack == null || stack.count <= 0) return 0;

        if (count < 0 || count >= stack.count)
        {
            int removed = stack.count;
            slots[slotIndex] = null;
            OnInventoryChanged?.Invoke();
            return removed;
        }

        stack.count -= count;
        OnInventoryChanged?.Invoke();
        return count;
    }

    public bool TryUse(int slotIndex)
    {
        var stack = GetSlot(slotIndex);
        if (stack == null || stack.data == null) return false;

        var action = stack.data.useAction;
        var user = ResolveUseTarget();
        if (action == null) return false;
        if (!action.TryUse(user)) return false;

        Remove(slotIndex, 1);
        return true;
    }

    public GameObject ResolveUseTarget()
    {
        if (useTarget != null) return useTarget;
        var cam = Camera.main;
        if (cam != null) return cam.transform.root.gameObject;
        return gameObject;
    }

    public Transform ResolveDropOrigin()
    {
        if (dropOrigin != null) return dropOrigin;
        var cam = Camera.main;
        if (cam != null) return cam.transform;
        return transform;
    }

    public bool TryDrop(int slotIndex, int count = -1)
    {
        var stack = GetSlot(slotIndex);
        if (stack == null || stack.data == null) return false;
        if (!stack.data.canDrop || stack.data.worldPrefab == null) return false;

        int toDrop = (count < 0) ? stack.count : Mathf.Min(count, stack.count);
        if (toDrop <= 0) return false;

        Transform origin = ResolveDropOrigin();
        Vector3 spawnPos = origin.position + origin.forward * dropForwardDistance + Vector3.up * dropUpOffset;
        Quaternion spawnRot = Quaternion.LookRotation(origin.forward, Vector3.up);

        var go = UnityEngine.Object.Instantiate(stack.data.worldPrefab, spawnPos, spawnRot);
        var instance = go.GetComponent<ItemInstance>();
        if (instance != null)
        {
            instance.data = stack.data;
            instance.count = toDrop;
        }
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(origin.forward * dropForwardForce, ForceMode.VelocityChange);
        }

        Remove(slotIndex, toDrop);
        return true;
    }
}

[Serializable]
public class ItemStack
{
    public ItemData data;
    public int count;
}
