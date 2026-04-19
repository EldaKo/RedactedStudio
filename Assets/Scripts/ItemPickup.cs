using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f; 
    public Camera mainCamera; 

    void Update()
    {
        // 플레이어가 F키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.F))
        {
            ItemInteraction();
        }
    }

    void ItemInteraction()
    {

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (hit.collider.CompareTag("Item"))
{
    // ItemInstance 컴포넌트 가져오기
    ItemInstance instance = hit.collider.GetComponent<ItemInstance>();
    
    if (instance == null || instance.data == null)
    {
        Debug.LogWarning($"[{hit.collider.gameObject.name}]에 ItemInstance 또는 ItemData가 없습니다.");
        return;
    }
    
    // 인벤토리에 넣기 시도
    bool added = Inventory.Instance.TryAdd(instance.data, instance.count);
    
    if (added)
    {
        Debug.Log($"[{instance.data.itemName}] x{instance.count} 획득");
        Destroy(hit.collider.gameObject);
    }
    else
    {
        Debug.Log("인벤토리가 가득 찼습니다.");
    }
}
            else
            {
                // 아이템이 아닌 다른 것(벽, 바닥 등)을 바라보고 F를 누른 경우 (디버그용)
                Debug.Log("상호작용할 수 없는 오브젝트입니다.");
            }
        }
    }
}
