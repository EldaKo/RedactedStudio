using UnityEngine;

public class ItemDisableWatcher : MonoBehaviour
{
    private IntroManager introManager;

    void Start()
    {
        introManager = FindObjectOfType<IntroManager>();
    }

    void OnDisable()
    {
        if (introManager != null)
        {
            introManager.StartGunSequence(); // ⭐ 여기만 호출
        }
    }
}