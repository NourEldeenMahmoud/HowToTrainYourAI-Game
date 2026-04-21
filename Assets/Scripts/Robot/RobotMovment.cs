using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RobotMovement : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform robotObj;
    [SerializeField] private MiniGame1FaultState miniGameFaultState;
    [SerializeField] private RobotStabilityApplier stabilityApplier;

    private CharacterController controller;
    private Animator anim;

    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 6.0f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    public float speedSmooth = 10f;

    private float currentSpeed;
    private float verticalVelocity;

    private Vector2 moveInput;
    private bool isSprinting;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = robotObj != null ? robotObj.GetComponentInParent<Animator>() : GetComponentInChildren<Animator>();

        if (miniGameFaultState == null)
            miniGameFaultState = GetComponent<MiniGame1FaultState>();

        if (stabilityApplier == null)
            stabilityApplier = GetComponent<RobotStabilityApplier>();

        if (stabilityApplier == null)
            stabilityApplier = gameObject.AddComponent<RobotStabilityApplier>();

        if (anim != null)
            anim.applyRootMotion = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetMovementEnabled(bool value)
    {
        enabled = value;
        if (!value)
        {
            moveInput = Vector2.zero;
            isSprinting = false;
            if (anim != null)
            {
                anim.SetBool("IsWalking", false);
                anim.SetBool("IsSprinting", false);
            }
        }
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

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
        if (orientation == null) return;

        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;

        float persistentYawDrift = stabilityApplier != null ? stabilityApplier.GetYawDriftDegrees() : 0f;
        float miniGameYawDrift = (miniGameFaultState != null && miniGameFaultState.faultsEnabled) ? miniGameFaultState.yawDriftDeg : 0f;
        float totalYawDrift = persistentYawDrift + miniGameYawDrift;

        if (Mathf.Abs(totalYawDrift) > 0.01f)
        {
            Quaternion driftRot = Quaternion.Euler(0f, totalYawDrift, 0f);
            forward = driftRot * forward;
            right = driftRot * right;
        }

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        moveDirection.y = 0f;

        bool hasMoveInput = moveInput.magnitude > 0.1f;
        bool shouldSprint = hasMoveInput && isSprinting;

        float targetSpeed = hasMoveInput ? (shouldSprint ? runSpeed : walkSpeed) : 0f;
        if (stabilityApplier != null)
            targetSpeed *= stabilityApplier.GetSpeedMultiplier();

        if (miniGameFaultState != null && miniGameFaultState.faultsEnabled)
            targetSpeed *= miniGameFaultState.GetSpeedMultiplier();

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedSmooth);

        if (anim != null)
        {
            anim.SetBool("IsWalking", hasMoveInput);
            anim.SetBool("IsSprinting", shouldSprint);
        }

        Transform visual = robotObj != null ? robotObj : transform;
        if (hasMoveInput)
        {
            Vector3 lookDir = moveDirection.normalized;
            visual.forward = Vector3.Slerp(visual.forward, lookDir, Time.deltaTime * rotationSpeed);
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = moveDirection.normalized * currentSpeed;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }
}