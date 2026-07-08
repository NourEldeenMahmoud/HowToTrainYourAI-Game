using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RobotFollowPlayerAfterMiniGame1 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private MiniGame1Manager miniGame1Manager;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private RobotMovement robotMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform rotationRoot;
    [SerializeField] private Animator robotAnimator;

    [Header("Follow")]
    [SerializeField, Min(0.1f)] private float followDistance = 2.0f;
    [SerializeField, Min(0.1f)] private float followSpeed = 3.0f;
    [SerializeField, Min(0f)] private float rotationSpeed = 8.0f;
    [SerializeField] private bool invertVisualForward;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Behavior")]
    [SerializeField] private bool autoFindReferences = true;

    [Header("Animation")]
    [SerializeField] private bool driveMovementAnimation = true;
    [SerializeField] private string walkingBoolParameter = "IsWalking";
    [SerializeField] private string sprintingBoolParameter = "IsSprinting";

    private bool isFollowing;
    private float verticalVelocity;
    private int walkingBoolHash;
    private int sprintingBoolHash;
    private bool hasWalkingBool;
    private bool hasSprintingBool;
    private Vector3 desiredLookDirection;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (robotMovement == null)
            robotMovement = GetComponent<RobotMovement>();

        ResolveReferences();
        ResolveVisualRefs();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResolveVisualRefs();
    }

    private void OnDisable()
    {
        SetFollowing(false);
    }

    private void Update()
    {
        if (autoFindReferences)
            ResolveReferences();

        bool shouldFollow = ShouldFollowPlayer();
        SetFollowing(shouldFollow);

        if (!isFollowing)
            return;

        FollowPlayer();
    }

    private void LateUpdate()
    {
        if (!isFollowing)
            return;

        if (desiredLookDirection.sqrMagnitude <= 0.0001f)
            return;

        Transform visual = GetVisualTransform();
        if (visual == null)
            return;

        Vector3 lookDir = desiredLookDirection.normalized;
        if (invertVisualForward)
            lookDir = -lookDir;
        visual.forward = Vector3.Slerp(visual.forward, lookDir, rotationSpeed * Time.deltaTime);
    }

    private void ResolveReferences()
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        if (miniGame1Manager == null)
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();

        if (playerTransform == null)
        {
            GameObject playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerTransform = playerGo.transform;
            }
            else
            {
                PlayerMovementAdvanced playerMovement = FindFirstObjectByType<PlayerMovementAdvanced>();
                if (playerMovement != null)
                    playerTransform = playerMovement.transform;
            }
        }

        ResolveVisualRefs();
    }

    private void ResolveVisualRefs()
    {
        if (robotMovement != null)
        {
            if (rotationRoot == null && robotMovement.robotObj != null)
                rotationRoot = robotMovement.robotObj;

            if (robotAnimator == null && robotMovement.robotObj != null)
                robotAnimator = robotMovement.robotObj.GetComponentInParent<Animator>();
        }

        if (rotationRoot == null)
            rotationRoot = transform;

        if (robotAnimator == null)
            robotAnimator = GetComponentInChildren<Animator>();

        hasWalkingBool = false;
        hasSprintingBool = false;
        if (robotAnimator == null)
            return;

        walkingBoolHash = Animator.StringToHash(walkingBoolParameter);
        sprintingBoolHash = Animator.StringToHash(sprintingBoolParameter);
        hasWalkingBool = HasAnimatorBool(robotAnimator, walkingBoolParameter);
        hasSprintingBool = HasAnimatorBool(robotAnimator, sprintingBoolParameter);
    }

    private static bool HasAnimatorBool(Animator animator, string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private bool ShouldFollowPlayer()
    {
        if (controlManager == null || miniGame1Manager == null || playerTransform == null)
            return false;

        if (controlManager.IsInputLocked)
            return false;

        if (!controlManager.IsPlayerControlActive)
            return false;

        return miniGame1Manager.CurrentPhase == MiniGame1Manager.MiniGame1Phase.Completed;
    }

    private void SetFollowing(bool following)
    {
        if (isFollowing == following)
            return;

        isFollowing = following;

        if (robotMovement != null)
            robotMovement.SetMovementEnabled(!isFollowing);

        if (!isFollowing)
        {
            SetMovementAnimation(false);
            verticalVelocity = 0f;
        }
    }

    private void FollowPlayer()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;
        Vector3 moveHorizontal = Vector3.zero;
        desiredLookDirection = Vector3.zero;
        if (distance > followDistance)
        {
            Vector3 direction = toPlayer / distance;
            float moveDistance = Mathf.Min(followSpeed * Time.deltaTime, distance - followDistance);
            moveHorizontal = direction * moveDistance;
            desiredLookDirection = direction;
        }

        SetMovementAnimation(moveHorizontal.sqrMagnitude > 0.000001f);

        if (characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 move = moveHorizontal;
            move.y = verticalVelocity * Time.deltaTime;
            characterController.Move(move);
            return;
        }

        transform.position += moveHorizontal;
    }

    private void SetMovementAnimation(bool isMoving)
    {
        if (!driveMovementAnimation || robotAnimator == null)
            return;

        if (hasWalkingBool)
            robotAnimator.SetBool(walkingBoolHash, isMoving);

        if (hasSprintingBool)
            robotAnimator.SetBool(sprintingBoolHash, false);
    }

    private Transform GetVisualTransform()
    {
        if (robotMovement != null && robotMovement.robotObj != null)
            return robotMovement.robotObj;

        if (rotationRoot != null)
            return rotationRoot;

        return transform;
    }
}
