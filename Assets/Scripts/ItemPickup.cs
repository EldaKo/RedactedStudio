using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3.5f;
    [Tooltip("조준 관용도. 0이면 정조준 raycast, >0이면 SphereCast 반경.")]
    public float aimTolerance = 0.18f;
    public Camera mainCamera;

    [Tooltip("InventoryUI 참조. 인벤토리가 열려 있을 때는 픽업을 막음. 비우면 항상 픽업 가능.")]
    public InventoryUI inventoryUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ItemInteraction();
        }
    }

    void ItemInteraction()
    {
        if (mainCamera == null) return;
        if (inventoryUI != null && inventoryUI.IsOpen) return;
        if (!Inventory.HasInstance) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!TryFindItem(ray, out var instance, out var hitGo)) return;
        if (instance == null || instance.data == null)
        {
            Debug.LogWarning($"[{hitGo.name}]에 ItemInstance 또는 ItemData가 없습니다.");
            return;
        }

        bool added = Inventory.Instance.TryAdd(instance.data, instance.count);
        if (added)
        {
            Debug.Log($"[{instance.data.itemName}] x{instance.count} 획득");
            Destroy(hitGo);
        }
        else
        {
            Debug.Log("인벤토리가 가득 찼습니다.");
        }
    }

    bool TryFindItem(Ray ray, out ItemInstance instance, out GameObject hitGo)
    {
        instance = null;
        hitGo = null;

        RaycastHit[] hits;
        if (aimTolerance > 0f)
            hits = Physics.SphereCastAll(ray, aimTolerance, interactRange, ~0, QueryTriggerInteraction.Collide);
        else
            hits = Physics.RaycastAll(ray, interactRange, ~0, QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;
            if (!col.CompareTag("Item")) continue;

            var inst = col.GetComponentInParent<ItemInstance>();
            if (inst == null) continue;

            instance = inst;
            hitGo = inst.gameObject;
            return true;
        }
        return false;
    }
}
