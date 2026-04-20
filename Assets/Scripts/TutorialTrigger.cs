using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [TextArea] 
    public string messageToDisplay;
    private IntroManager introManager;

    void Start()
    {
        introManager = FindObjectOfType<IntroManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 태그를 가진 오브젝트가 들어왔을 때만!
        if (other.CompareTag("Player"))
        {
            introManager.StartMoveSequence();
        }
    }
}