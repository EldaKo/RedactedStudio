using UnityEngine;
using System.Collections;

public class HideoutCamera : MonoBehaviour
{
    public static HideoutCamera Instance;

    private Camera mainCam;
    
    [Header("카메라 설정")]
    public float transitionTime = 1.0f; // 카메라 이동에 걸리는 시간

    // 돌아갈 원래 탑뷰 위치와 회전값 저장용
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    private Coroutine moveCoroutine;

    void Awake()
    {
        Instance = this;
        mainCam = Camera.main;

        // 게임 시작 시점의 카메라(탑뷰) 위치와 회전값을 저장해 둡니다.
        originalPosition = mainCam.transform.position;
        originalRotation = mainCam.transform.rotation;
    }

    // 특정 시설의 뷰포인트로 이동
    public void MoveToFacility(Transform targetViewPoint)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCameraRoutine(targetViewPoint.position, targetViewPoint.rotation));
    }

    // 원래 탑뷰로 복귀
    public void ReturnToTopView()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCameraRoutine(originalPosition, originalRotation));
    }

    // 카메라를 부드럽게 이동시키는 코루틴
    private IEnumerator MoveCameraRoutine(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < transitionTime)
        {
            // SmoothStep 공식 (자연스러운 가감속: 서서히 출발하고 서서히 멈춤)
            float t = elapsedTime / transitionTime;
            t = t * t * (3f - 2f * t);

            // 위치와 회전을 동시에 부드럽게 변경
            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCam.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 오차 보정 (완전히 목표 지점에 안착)
        mainCam.transform.position = targetPos;
        mainCam.transform.rotation = targetRot;
    }
}