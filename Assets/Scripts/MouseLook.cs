using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    public float mouseSensitivity = 50f;
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensValueText;

    [Header("Speed Multiplier")]
    // 이 값을 1.0으로 두면, 슬라이더 100일 때 화면이 미친듯이 돌아갈 겁니다.
    // 50 정도가 지금 본인이 느끼는 '최대치'가 되도록 이 값을 조절해 보세요.
    public float sensitivityMultiplier = 0.01f;

    [Header("References")]
    public Transform playerBody;

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1;
            sensitivitySlider.maxValue = 100;
            sensitivitySlider.value = mouseSensitivity;
        }
    }

    // MouseLook.cs의 Update 함수 내부 수정
    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (sensitivitySlider != null)
        {
            mouseSensitivity = sensitivitySlider.value;
            if (sensValueText != null)
                sensValueText.text = mouseSensitivity.ToString();
        }

        // [수정된 부분] 
        // finalSens가 너무 뻥튀기되지 않도록 multiplier를 아주 작게 잡거나 계산식을 조정합니다.
        // 슬라이더 100일 때 적당히 빠르려면 0.01f ~ 0.05f 정도가 적당합니다.
        float finalSens = mouseSensitivity * sensitivityMultiplier;

        // 기존 100f나 10f 곱하기를 제거하고 finalSens만 믿고 갑니다.
        float mouseX = Input.GetAxis("Mouse X") * finalSens;
        float mouseY = Input.GetAxis("Mouse Y") * finalSens;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}