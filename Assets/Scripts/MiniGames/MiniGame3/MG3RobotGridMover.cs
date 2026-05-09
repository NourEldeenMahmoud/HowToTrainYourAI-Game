using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MG3RobotGridMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MG3GridManager gridManager;
    [SerializeField] private MG3Pathfinder pathfinder;
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private Transform moverTransform;
    [SerializeField] private Transform rotationTransform;

    [Header("Input")]
    [SerializeField] private InputActionReference clickMoveAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private LayerMask floorLayer;
    [SerializeField, Min(1f)] private float clickMaxDistance = 250f;
    [SerializeField] private bool blockClicksWhenPointerOverUi = true;
    [SerializeField, Min(0.01f)] private float clickToTileMaxDistance = 0.8f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(30f)] private float rotationSpeedDegrees = 540f;
    [SerializeField, Min(0.01f)] private float arriveDistance = 0.03f;
    [SerializeField] private bool rotateTowardsMovement = true;

    [Header("Animation")]
    [SerializeField] private Animator robotAnimator;
    [SerializeField] private string walkingBoolParameter = "IsWalking";
    [SerializeField] private string pushingTriggerParameter = "Push";
    [SerializeField] private string pushingBoolParameter = "IsPushing";

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private bool suppressRepeatedIgnoredClickLogs = true;
    [SerializeField, Min(0f)] private float ignoredClickLogCooldown = 0.25f;

    public event Action<Vector2Int, string> DestinationRejected;
    public event Action<Vector2Int> DestinationReached;
    public event Action<Vector2Int> DestinationRequested;
    public event Action<Vector2Int, Vector2Int> MovementStarted;

    public bool IsMoving { get; private set; }
    public bool IsMovementLockedBySystem { get; private set; }
    public Vector2Int CurrentGridCoord { get; private set; }

    private Coroutine moveRoutine;
    private int walkingBoolHash;
    private int pushingTriggerHash;
    private int pushingBoolHash;
    private bool hasWalkingBool;
    private bool hasPushingTrigger;
    private bool hasPushingBool;
    private string lastIgnoredReason;
    private float lastIgnoredLogTime;
    private Transform Mover => moverTransform != null ? moverTransform : transform;
    private Transform RotationTarget => rotationTransform != null ? rotationTransform : Mover;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<MG3GridManager>();
        }

        if (pathfinder == null)
        {
            pathfinder = FindFirstObjectByType<MG3Pathfinder>();
        }

        ResolveAnimator();
    }

    private void OnEnable()
    {
        if (clickMoveAction != null && clickMoveAction.action != null)
        {
            clickMoveAction.action.performed += OnClickMovePerformed;
        }
    }

    private void OnDisable()
    {
        if (clickMoveAction != null && clickMoveAction.action != null)
        {
            clickMoveAction.action.performed -= OnClickMovePerformed;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        IsMoving = false;
        SetWalkingAnimation(false);
        if (gridManager != null)
        {
            gridManager.UnregisterOccupant(this);
        }
    }

    private void Start()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }

        if (gridManager == null || pathfinder == null)
        {
            Debug.LogError("[MG3RobotGridMover] Missing MG3GridManager or MG3Pathfinder reference.", this);
            enabled = false;
            return;
        }

        if (gridManager.TileCount == 0)
        {
            gridManager.BuildRegistry();
        }
        gridManager.SyncPushableOccupancyFromScene(false);

        CurrentGridCoord = gridManager.WorldToGrid(Mover.position);
        if (!gridManager.IsInBounds(CurrentGridCoord))
        {
            if (gridManager.TryFindNearestTileCoord(Mover.position, out Vector2Int nearestCoord))
            {
                CurrentGridCoord = nearestCoord;
            }
            else
            {
                Debug.LogError("[MG3RobotGridMover] Grid has no registered tiles.", this);
                enabled = false;
                return;
            }
        }

        Mover.position = gridManager.GridToWorld(CurrentGridCoord);
        if (!gridManager.RegisterOccupant(this, CurrentGridCoord, MG3GridManager.OccupantKind.Robot))
        {
            Debug.LogWarning($"[MG3RobotGridMover] Could not register robot at {CurrentGridCoord}.", this);
        }

        // Ensure all pushable devices have consistent occupancy after initialisation
        MG3PushableDevice[] allDevices = FindObjectsByType<MG3PushableDevice>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var device in allDevices)
        {
            device.ValidateOccupancyConsistency();
        }
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }

        ProcessClick(Mouse.current.position.ReadValue());
    }

    public void SetMovementLock(bool locked)
    {
        IsMovementLockedBySystem = locked;
    }

    public void PlayPushAnimation()
    {
        if (robotAnimator != null && hasPushingTrigger)
        {
            robotAnimator.SetTrigger(pushingTriggerHash);
        }
    }

    public void SetPushingAnimation(bool isPushing)
    {
        if (robotAnimator != null && hasPushingBool)
        {
            robotAnimator.SetBool(pushingBoolHash, isPushing);
        }

        if (isPushing)
        {
            SetWalkingAnimation(false);
        }
    }

    public void DebugLogAnimatorSnapshot(string tag)
    {
        if (!verboseLogs)
        {
            return;
        }

        if (robotAnimator == null)
        {
            Debug.Log($"[MG3RobotGridMover] {tag} animator=null", this);
            return;
        }

        AnimatorStateInfo state = robotAnimator.GetCurrentAnimatorStateInfo(0);
        string stateName = state.IsName("Push") ? "Push" : state.shortNameHash.ToString();
        bool pushingValue = hasPushingBool && robotAnimator.GetBool(pushingBoolHash);
        bool walkingValue = hasWalkingBool && robotAnimator.GetBool(walkingBoolHash);

        Debug.Log($"[MG3RobotGridMover] {tag} animator='{robotAnimator.name}' layer0='{stateName}' normalizedTime={state.normalizedTime:0.00} IsPushing={pushingValue} IsWalking={walkingValue}", this);
    }

    public void FaceDirection(Vector3 worldDirection)
    {
        Vector3 flat = new Vector3(worldDirection.x, 0f, worldDirection.z);
        if (flat.sqrMagnitude < 0.000001f)
        {
            return;
        }

        RotationTarget.rotation = Quaternion.LookRotation(flat.normalized);
    }

    public void WarpToCoordinate(Vector2Int coord)
    {
        if (gridManager == null)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        IsMoving = false;
        gridManager.UnregisterOccupant(this);
        CurrentGridCoord = coord;
        Mover.position = gridManager.GridToWorld(coord);
        gridManager.RegisterOccupant(this, CurrentGridCoord, MG3GridManager.OccupantKind.Robot);
    }

    public bool TryRequestMove(Vector2Int destination)
    {
        if (IsMovementLockedBySystem || IsMoving)
        {
            return false;
        }

        DestinationRequested?.Invoke(destination);

        if (!pathfinder.TryFindPath(CurrentGridCoord, destination, out List<Vector2Int> path, out string reason))
        {
            DestinationRejected?.Invoke(destination, reason);
            Debug.Log($"[MG3RobotGridMover] Destination rejected {destination}: {reason}", this);
            return false;
        }

        if (path == null || path.Count <= 1)
        {
            return false;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        MovementStarted?.Invoke(CurrentGridCoord, destination);
        moveRoutine = StartCoroutine(MovePath(path));
        return true;
    }

    private void OnClickMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 screenPos = ResolvePointerScreenPosition(context);
        if (screenPos != Vector2.negativeInfinity)
        {
            ProcessClick(screenPos);
        }
    }

    private Vector2 ResolvePointerScreenPosition(InputAction.CallbackContext context)
    {
        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            return pointerPositionAction.action.ReadValue<Vector2>();
        }

        if (context.control != null && context.control.device is Pointer pointer)
        {
            return pointer.position.ReadValue();
        }

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.negativeInfinity;
    }

    private void ProcessClick(Vector2 screenPos)
    {
        if (IsMoving || IsMovementLockedBySystem)
        {
            LogIgnoredClick("movement locked or robot is moving");
            return;
        }

        if (blockClicksWhenPointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
        {
            LogIgnoredClick("pointer over UI");
            return;
        }

        if (!TryResolveClickedCoord(screenPos, out Vector2Int destination))
        {
            LogIgnoredClick("no valid tile resolved from raycast");
            return;
        }

        TryRequestMove(destination);
    }

    private bool TryResolveClickedCoord(Vector2 screenPos, out Vector2Int destination)
    {
        destination = default;

        if (raycastCamera == null)
        {
            return false;
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;
        bool hitSomething = floorLayer.value == 0
            ? Physics.Raycast(ray, out hit, clickMaxDistance)
            : Physics.Raycast(ray, out hit, clickMaxDistance, floorLayer, QueryTriggerInteraction.Ignore);

        if (!hitSomething)
        {
            return false;
        }

        if (!gridManager.TryGetTileCoordFromWorld(hit.point, clickToTileMaxDistance, out destination))
        {
            return false;
        }

        return true;
    }

    private IEnumerator MovePath(List<Vector2Int> path)
    {
        IsMoving = true;
        SetWalkingAnimation(true);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int next = path[i];
            if (!gridManager.MoveOccupant(this, next))
            {
                DestinationRejected?.Invoke(next, "Path step became occupied");
                Debug.LogWarning($"[MG3RobotGridMover] Movement interrupted; step {next} is unavailable.", this);
                break;
            }

            Vector3 target = gridManager.GridToWorld(next);
            while (Vector3.Distance(Mover.position, target) > arriveDistance)
            {
                Vector3 toTarget = target - Mover.position;
                Vector3 move = toTarget.normalized * (moveSpeed * Time.deltaTime);
                if (move.sqrMagnitude > toTarget.sqrMagnitude)
                {
                    move = toTarget;
                }

                Mover.position += move;

                if (rotateTowardsMovement && move.sqrMagnitude > 0.000001f)
                {
                    Quaternion desired = Quaternion.LookRotation(new Vector3(move.x, 0f, move.z));
                    RotationTarget.rotation = Quaternion.RotateTowards(RotationTarget.rotation, desired, rotationSpeedDegrees * Time.deltaTime);
                }

                yield return null;
            }

            Mover.position = target;
            CurrentGridCoord = next;
        }

        IsMoving = false;
        SetWalkingAnimation(false);
        moveRoutine = null;
        DestinationReached?.Invoke(CurrentGridCoord);
    }

    private void ResolveAnimator()
    {
        if (robotAnimator == null)
        {
            robotAnimator = Mover.GetComponentInChildren<Animator>(true);
        }

        hasWalkingBool = false;
        hasPushingTrigger = false;
        hasPushingBool = false;
        if (robotAnimator == null)
        {
            return;
        }

        walkingBoolHash = Animator.StringToHash(walkingBoolParameter);
        pushingTriggerHash = Animator.StringToHash(pushingTriggerParameter);
        pushingBoolHash = Animator.StringToHash(pushingBoolParameter);

        AnimatorControllerParameter[] parameters = robotAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];
            if (p.name == walkingBoolParameter && p.type == AnimatorControllerParameterType.Bool)
            {
                hasWalkingBool = true;
            }

            if (p.name == pushingTriggerParameter && p.type == AnimatorControllerParameterType.Trigger)
            {
                hasPushingTrigger = true;
            }

            if (p.name == pushingBoolParameter && p.type == AnimatorControllerParameterType.Bool)
            {
                hasPushingBool = true;
            }
        }
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (robotAnimator != null && hasWalkingBool)
        {
            robotAnimator.SetBool(walkingBoolHash, isWalking);
        }
    }

    private void LogIgnoredClick(string reason)
    {
        if (!verboseLogs)
        {
            return;
        }

        if (suppressRepeatedIgnoredClickLogs && reason == lastIgnoredReason && (Time.unscaledTime - lastIgnoredLogTime) < ignoredClickLogCooldown)
        {
            return;
        }

        lastIgnoredReason = reason;
        lastIgnoredLogTime = Time.unscaledTime;
        Debug.Log($"[MG3RobotGridMover] Click ignored: {reason}.", this);
    }
}
