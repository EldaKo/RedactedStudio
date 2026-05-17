using UnityEngine;

public class IntroManager : MonoBehaviour
{
    [Header("OBJ setting")]
    public GameObject targetObject;

    public void StartMoveSequence() { }

    public void StartGunSequence()
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }
}
