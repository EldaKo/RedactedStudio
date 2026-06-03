using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Display")]
    public string itemName;
    public Sprite icon;
    [TextArea(2, 5)] public string description;

    [Header("Stack")]
    public int maxStack = 64;

    [Header("Use / Drop")]
    public ItemUseAction useAction;
    public GameObject worldPrefab;
    public bool canDrop = true;

    public bool CanUse(GameObject user) => useAction != null && useAction.CanUse(user);
}
