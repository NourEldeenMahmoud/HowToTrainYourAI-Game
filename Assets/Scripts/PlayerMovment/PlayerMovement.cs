using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementAdvanced : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    private CharacterController controller;
    private Animator anim;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float speedSmooth = 10f;

    private float currentSpeed;
    private float verticalVelocity;

    [Header("Camera")]
    public GameObject basicCam;

    private Vector2 moveInput;
    private bool isSprinting;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = playerObj != null ? playerObj.GetComponent<Animator>() : GetComponentInChildren<Animator>();

        if (basicCam != null) basicCam.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // استقبال الحركة من الـ Input System
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // استقبال الجري (Shift) من الـ Input System
    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    void Update()
    {
        RotateOrientation();
        ApplyMovement();
    }

    void RotateOrientation()
    {
        if (Camera.main == null || orientation == null) return;

        Vector3 viewDir = transform.position - new Vector3(
            Camera.main.transform.position.x,
            transform.position.y,
            Camera.main.transform.position.z
        );

        orientation.forward = viewDir.normalized;
    }

    void ApplyMovement()
    {
        if (orientation == null || playerObj == null) return;

        Vector3 moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;
        bool hasMoveInput = moveInput.magnitude > 0.1f;
        bool shouldSprint = hasMoveInput && isSprinting;

        float targetSpeed = hasMoveInput ? (shouldSprint ? sprintSpeed : walkSpeed) : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedSmooth);

        if (anim != null)
        {
            anim.SetBool("IsWalking", hasMoveInput);
            anim.SetBool("IsSprinting", shouldSprint);
        }

        // Basic style rotation only
        if (hasMoveInput)
        {
            Vector3 lookDir = moveDirection.normalized;
            playerObj.forward = Vector3.Slerp(playerObj.forward, lookDir, Time.deltaTime * rotationSpeed);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = moveDirection.normalized * currentSpeed;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }
}