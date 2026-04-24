using UnityEngine;
using TMPro;
using System.Collections;

public class IngameScript : MonoBehaviour
{
    [Header("OBJ setting")]
    public GameObject targetObject;


    public TextMeshProUGUI subtitleText;
    public float typingSpeed = 0.05f;

    void Start()
    {
        StartCoroutine(SequenceIntro());
    }

    // 🔥 기본 인트로
    IEnumerator SequenceIntro()
    {
        yield return StartCoroutine(TypeText("환영합니다. 당신은 어느 버려진 도시에 도착하였습니다."));
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(TypeText("당신의 목표는 하나. 물건을 수거하여 지정된 장소에 보급하는 것."));
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(TypeText("다만, 이 도시에는 당신을 환영하지 않는 것들이 존재하는듯 하군요."));

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(TypeText("다행이도 바닥에는 당신을 보조할 여러 물건들이 떨어져있습니다. 건투를 빕니다."));
        yield return new WaitForSeconds(2f);
    }

    public void StartMoveSequence()
    {
        StopAllCoroutines();
        StartCoroutine(MoveSequence());
    }


    public void StartGunSequence()
    {
        StopAllCoroutines();
        StartCoroutine(GunSequence());
    }

   
    IEnumerator MoveSequence()
    {
        yield return StartCoroutine(TypeText("잘했습니다. 이제 F키를 눌러 눈 앞의 총기를 주워봅시다."));

    }

    IEnumerator GunSequence()
    {
        if (targetObject != null)
            targetObject.SetActive(true);

        yield return StartCoroutine(TypeText("총기를 획득했습니다. 좌클릭으로 발사할 수 있습니다."));

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(TypeText("이제 반짝이는 과녁을 향해 총기를 발사해봅시다."));
    }

    IEnumerator TypeText(string message)
    {
        subtitleText.text = "";

        foreach (char letter in message)
        {
            subtitleText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
