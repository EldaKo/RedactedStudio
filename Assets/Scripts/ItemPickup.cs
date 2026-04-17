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
                Debug.Log($"[{hit.collider.gameObject.name}] 획득");

                Destroy(hit.collider.gameObject);
            }
            else
            {
                // 아이템이 아닌 다른 것(벽, 바닥 등)을 바라보고 F를 누른 경우 (디버그용)
                Debug.Log("상호작용할 수 없는 오브젝트입니다.");
            }
        }
    }
}
