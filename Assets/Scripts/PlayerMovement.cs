using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody rb;
    public CapsuleCollider col;

    private float walkSpeed = 5f;
    private float crouchSpeed = 4f;
    private float jumpForce = 3f;
    private float fallMultiplier = 5f;

    [Header("Crouch Settings")]
    public float normalHeight = 2f;
    public float crouchHeight = 1f;
    public Transform cameraTransform;
    public float cameraNormalY = 0.8f;
    public float cameraCrouchY = 0.4f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    bool isGrounded;
    float currentSpeed;

    void Start()
    {
        rb.freezeRotation = true;
        rb.useGravity = true;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        HandleCrouch();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        Move();
        ApplyBetterFall();
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        float finalSpeed = currentSpeed;
        if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
        {
            finalSpeed *= 1.5f;
        }

        Vector3 targetVelocity = move * finalSpeed;

        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
    }

    void ApplyBetterFall()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            col.height = crouchHeight;
            currentSpeed = crouchSpeed;
            cameraTransform.localPosition = new Vector3(0, cameraCrouchY, 0);
        }
        else
        {
            col.height = normalHeight;
            currentSpeed = walkSpeed;
            cameraTransform.localPosition = new Vector3(0, cameraNormalY, 0);
        }
    }
}