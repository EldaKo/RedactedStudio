using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Data Registry", fileName = "ItemDataRegistry")]
public class ItemDataRegistry : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();

    private static ItemDataRegistry _cached;

    public static ItemDataRegistry Get()
    {
        if (_cached == null) _cached = Resources.Load<ItemDataRegistry>("ItemDataRegistry");
        return _cached;
    }

    public ItemData FindByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || items == null) return null;
        foreach (var it in items)
            if (it != null && it.itemName == itemName) return it;
        return null;
    }
}
