using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class TileClickMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MiniGame2Manager miniGame2Manager;
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private Animator robotAnimator;
    [Tooltip("If set, this transform is moved (e.g. the robot root). If null, this object moves itself.")]
    [SerializeField] private Transform moverTransform;
    [Tooltip("Optional visual transform to rotate toward movement direction. If null, mover transform is used.")]
    [SerializeField] private Transform rotationTransform;

    [Header("Input")]
    [SerializeField] private InputActionReference clickMoveAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private LayerMask floorLayer;
    [SerializeField, Min(0f)] private float clickMaxDistance = 200f;
    [Tooltip("If true, ignores right-clicks while the pointer is over a UI element.")]
    [SerializeField] private bool blockClicksWhenPointerOverUi = false;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 3.5f;
    [SerializeField] private bool rotateTowardsMovement = true;
    [SerializeField, Min(1f)] private float rotationSpeedDegrees = 540f;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField] private int maxQueuedTargets = 10;
    [Tooltip("If true, you can only click a tile adjacent to the current tile (no long-distance path).")]
    [SerializeField] private bool restrictToAdjacentTileOnly = true;
    [Tooltip("If true, diagonal adjacent moves are allowed.")]
    [SerializeField] private bool allowDiagonalAdjacent = true;

    [Header("Step Recording (No Rigidbody needed)")]
    [Tooltip("If enabled, each reached step is recorded directly on MiniGame2Manager (energy/path/win) without requiring floor trigger colliders.")]
    [SerializeField] private bool recordStepsDirectly = true;

    [Header("Animation")]
    [SerializeField] private bool driveMovementAnimation = true;
    [SerializeField] private string walkingBoolParameter = "IsWalking";
    [SerializeField] private string sprintingBoolParameter = "IsSprinting";

    [Header("Debug")]
    [SerializeField] private bool verboseMovementLogs = false;

    private enum MovementState
    {
        Idle,
        Moving
    }

    private readonly Queue<Vector2Int> stepQueue = new Queue<Vector2Int>();
    private Coroutine processRoutine;
    private MovementState movementState = MovementState.Idle;
    private CharacterController moverCharacterController;
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private bool hasCurrentGridPos;
    private int currentStepToken;
    private int lastCompletedStepToken;
    private int totalExecutedSteps;
    private float totalAppliedEnergy;
    private float totalExpectedEnergyFromGrid;
    private readonly List<Vector2Int> visitedStepCoords = new List<Vector2Int>(256);
    private int lastProcessedClickFrame = -1;
    private int walkingBoolHash;
    private int sprintingBoolHash;
    private bool hasWalkingBool;
    private bool hasSprintingBool;
    /// <summary>Transform actually moved (CC transform if found on self/children).</summary>
    private Transform moveRoot;

    public event Action<Vector2Int, float> StepCompleted;

    public Vector2Int CurrentGridPos => currentGridPos;
    public bool IsMoving => movementState == MovementState.Moving;
    public bool AllowDiagonalAdjacent => allowDiagonalAdjacent;
    public int QueuedStepCount => stepQueue.Count;
    public int TotalExecutedSteps => totalExecutedSteps;
    public float TotalAppliedEnergy => totalAppliedEnergy;
    public float TotalExpectedEnergyFromGrid => totalExpectedEnergyFromGrid;
    public IReadOnlyList<Vector2Int> VisitedStepCoords => visitedStepCoords;

    private Transform Mover => moverTransform != null ? moverTransform : transform;
    private Transform RotationTarget => rotationTransform != null ? rotationTransform : MoverRoot;

    /// <summary>Transform that actually moves (same as moverTransform target, or CC transform).</summary>
    public Transform MoverRoot => moveRoot != null ? moveRoot : Mover;

    private void Awake()
    {
        ResolveMoveRoot();
        ResolveAnimator();
    }

    private void OnEnable()
    {
        BindInputActions();
        if (processRoutine == null)
            processRoutine = StartCoroutine(ProcessQueue());
    }

    private void OnDisable()
    {
        if (processRoutine != null)
        {
            StopCoroutine(processRoutine);
            processRoutine = null;
        }

        UnbindInputActions();
        stepQueue.Clear();
        movementState = MovementState.Idle;
        SetMovementAnimation(false);
    }

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (miniGame2Manager == null) miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
        if (raycastCamera == null) raycastCamera = Camera.main;
        ResolveMoveRoot();
        ResolveAnimator();
        InitializeRuntimeGridPosition();
    }

    private void ResolveMoveRoot()
    {
        moverCharacterController = Mover.GetComponent<CharacterController>();
        if (moverCharacterController == null)
            moverCharacterController = Mover.GetComponentInChildren<CharacterController>(true);
        moveRoot = moverCharacterController != null ? moverCharacterController.transform : Mover;
    }

    private void ResolveAnimator()
    {
        if (robotAnimator == null)
            robotAnimator = Mover.GetComponentInChildren<Animator>(true);
        if (robotAnimator == null)
            robotAnimator = Mover.GetComponentInParent<Animator>();

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

    private void SetMovementAnimation(bool isMoving)
    {
        if (!driveMovementAnimation || robotAnimator == null)
            return;

        if (hasWalkingBool)
            robotAnimator.SetBool(walkingBoolHash, isMoving);
        if (hasSprintingBool)
            robotAnimator.SetBool(sprintingBoolHash, false);
    }

    private void BindInputActions()
    {
        if (clickMoveAction != null && clickMoveAction.action != null)
            clickMoveAction.action.performed += OnClickMovePerformed;

        if (interactAction != null && interactAction.action != null)
            interactAction.action.performed += OnInteractPerformed;
    }

    private void UnbindInputActions()
    {
        if (clickMoveAction != null && clickMoveAction.action != null)
            clickMoveAction.action.performed -= OnClickMovePerformed;

        if (interactAction != null && interactAction.action != null)
            interactAction.action.performed -= OnInteractPerformed;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (miniGame2Manager == null)
            return;

        miniGame2Manager.TryInteractWithAudioCard(currentGridPos);
    }

    private void OnClickMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 screenPos = ResolvePointerScreenPosition(context);
        if (screenPos == Vector2.negativeInfinity)
            return;

        if (blockClicksWhenPointerOverUi && EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(-1))
            return;

        ProcessClick(screenPos, "action");
    }

    private Vector2 ResolvePointerScreenPosition(InputAction.CallbackContext context)
    {
        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            InputAction pointerAction = pointerPositionAction.action;
            if (string.Equals(pointerAction.expectedControlType, "Vector2", StringComparison.OrdinalIgnoreCase))
                return pointerAction.ReadValue<Vector2>();
        }

        if (context.control != null && context.control.device is Pointer pointer)
            return pointer.position.ReadValue();

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.negativeInfinity;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        if (blockClicksWhenPointerOverUi && EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(-1))
            return;

        ProcessClick(Mouse.current.position.ReadValue(), "mouse");
    }

    private void ProcessClick(Vector2 screenPos, string source)
    {
        if (lastProcessedClickFrame == Time.frameCount)
            return;

        lastProcessedClickFrame = Time.frameCount;
        if (verboseMovementLogs)
            miniGame2Manager?.LogPathEvent($"[MG2] Click received from {source} at {screenPos}");

        HandleClick(screenPos);
    }

    private void InitializeRuntimeGridPosition()
    {
        if (gridManager == null) return;

        if (miniGame2Manager != null && miniGame2Manager.TryGetStartCoord(out Vector2Int startCoord))
        {
            currentGridPos = startCoord;
        }
        else
        {
            currentGridPos = gridManager.WorldToGrid(moveRoot.position);
        }

        targetGridPos = currentGridPos;
        hasCurrentGridPos = true;
        currentStepToken = 0;
        lastCompletedStepToken = 0;
        totalExecutedSteps = 0;
        totalAppliedEnergy = 0f;
        totalExpectedEnergyFromGrid = 0f;
        visitedStepCoords.Clear();
        SnapVisualToGrid(currentGridPos);
    }

    private void HandleClick(Vector2 screenPos)
    {
        if (!TryResolveTargetGrid(screenPos, out Vector2Int destination))
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent("[MG2] Click ignored: no floor hit / unresolved grid destination");
            return;
        }

        if (verboseMovementLogs)
            miniGame2Manager?.LogPathEvent($"[MG2] Resolved destination {destination.x},{destination.y} from click");

        TryRequestMoveToGrid(destination);
    }

    public bool TryRequestMoveToGrid(Vector2Int destination)
    {
        if (movementState != MovementState.Idle)
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent($"[MG2] Move request rejected: mover not idle ({movementState})");
            return false;
        }

        if (!hasCurrentGridPos)
            InitializeRuntimeGridPosition();

        if (!ValidateTarget(destination))
            return false;

        List<Vector2Int> pathSteps = BuildPath(destination);
        if (pathSteps == null || pathSteps.Count == 0)
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent($"[MG2] Move request rejected: empty path to {destination.x},{destination.y}");
            return false;
        }

        CommitMovePlan(pathSteps);
        return true;
    }

    private bool TryResolveTargetGrid(Vector2 screenPos, out Vector2Int destination)
    {
        destination = default;
        if (raycastCamera == null) return false;

        if (hasCurrentGridPos && restrictToAdjacentTileOnly)
        {
            if (TryProjectClickToMovementPlane(screenPos, out Vector3 projectedPoint))
            {
                destination = InferAdjacentFromClick(projectedPoint, currentGridPos);
                return true;
            }
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPos);
        if (!TryGetFloorHit(ray, out RaycastHit hit))
            return false;

        if (gridManager == null) return false;

        FloorTile floorTile = hit.collider != null ? hit.collider.GetComponentInParent<FloorTile>() : null;
        if (floorTile != null)
        {
            destination = floorTile.GridCoord;
        }
        else
        {
            destination = gridManager.WorldToGrid(hit.point);
        }

        if (hasCurrentGridPos && restrictToAdjacentTileOnly && destination == currentGridPos)
            destination = InferAdjacentFromClick(hit.point, currentGridPos);

        return true;
    }

    private bool TryProjectClickToMovementPlane(Vector2 screenPos, out Vector3 worldPoint)
    {
        worldPoint = default;
        if (raycastCamera == null)
            return false;

        float planeY = moveRoot != null ? moveRoot.position.y : 0f;
        Plane movementPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        Ray ray = raycastCamera.ScreenPointToRay(screenPos);
        if (!movementPlane.Raycast(ray, out float distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        return true;
    }

    private Vector2Int InferAdjacentFromClick(Vector3 worldHit, Vector2Int from)
    {
        if (gridManager == null)
            return from;

        Vector3 fromWorld = gridManager.GridToWorld(from.x, from.y);
        Vector3 delta = worldHit - fromWorld;
        delta.y = 0f;

        float deadZone = Mathf.Max(0.05f, gridManager == null ? 0.1f : 0.15f * Mathf.Max(0.01f, GetCellSizeGuess()));
        int stepX = Mathf.Abs(delta.x) > deadZone ? (delta.x > 0f ? 1 : -1) : 0;
        int stepY = Mathf.Abs(delta.z) > deadZone ? (delta.z > 0f ? 1 : -1) : 0;

        if (allowDiagonalAdjacent && stepX != 0 && stepY != 0)
        {
            float absX = Mathf.Abs(delta.x);
            float absZ = Mathf.Abs(delta.z);
            if (absX > absZ * 1.35f) stepY = 0;
            else if (absZ > absX * 1.35f) stepX = 0;
        }

        if (!allowDiagonalAdjacent)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z)) stepY = 0;
            else stepX = 0;
        }

        if (stepX == 0 && stepY == 0)
            return from;

        return new Vector2Int(from.x + stepX, from.y + stepY);
    }

    private float GetCellSizeGuess()
    {
        Vector3 a = gridManager.GridToWorld(0, 0);
        Vector3 b = gridManager.GridToWorld(1, 0);
        return Mathf.Abs(b.x - a.x);
    }

    private bool ValidateTarget(Vector2Int destination)
    {
        if (gridManager == null || !hasCurrentGridPos)
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent("[MG2] Validate failed: grid not ready or currentGridPos not initialized");
            return false;
        }

        GridManager.Node destinationNode = gridManager.GetNode(destination.x, destination.y);
        if (destinationNode == null || destinationNode.isBlocked)
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent($"[MG2] Validate failed: destination invalid or blocked ({destination.x},{destination.y})");
            return false;
        }

        if (destination == currentGridPos)
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent($"[MG2] Validate ignored: clicked current tile {destination.x},{destination.y}");
            return false;
        }

        if (restrictToAdjacentTileOnly)
        {
            int dx = Mathf.Abs(destination.x - currentGridPos.x);
            int dy = Mathf.Abs(destination.y - currentGridPos.y);
            bool isAdjacent = allowDiagonalAdjacent ? (dx <= 1 && dy <= 1 && (dx + dy) > 0) : ((dx + dy) == 1);
            if (!isAdjacent)
            {
                miniGame2Manager?.LogPathEvent("[MG2] You can only move to an adjacent tile");
                return false;
            }
        }

        if (maxQueuedTargets > 0 && stepQueue.Count >= maxQueuedTargets)
        {
            if (verboseMovementLogs)
                miniGame2Manager?.LogPathEvent("[MG2] Validate failed: queue is full");
            return false;
        }

        return true;
    }

    private List<Vector2Int> BuildPath(Vector2Int destination)
    {
        if (restrictToAdjacentTileOnly)
            return new List<Vector2Int>(1) { destination };

        List<Vector2Int> fullPath = gridManager.FindIdealPath(currentGridPos, destination);
        if (fullPath == null || fullPath.Count <= 1)
            return null;

        fullPath.RemoveAt(0);
        return fullPath;
    }

    private void CommitMovePlan(List<Vector2Int> pathSteps)
    {
        int added = 0;
        for (int i = 0; i < pathSteps.Count; i++)
        {
            if (maxQueuedTargets > 0 && stepQueue.Count >= maxQueuedTargets)
                break;

            stepQueue.Enqueue(pathSteps[i]);
            added++;
        }

        if (added > 0)
        {
            Vector2Int last = pathSteps[Mathf.Min(pathSteps.Count - 1, added - 1)];
            string capacity = maxQueuedTargets > 0 ? maxQueuedTargets.ToString() : "inf";
            miniGame2Manager?.LogPathEvent($"[MG2] Queued {added} step(s) toward {last.x},{last.y} ({stepQueue.Count}/{capacity})");
        }
    }

    /// <summary>
    /// FloorLayer = 0 means "nothing" in Unity; use RaycastAll fallback.
    /// </summary>
    private bool TryGetFloorHit(Ray ray, out RaycastHit hit)
    {
        hit = default;
        int mask = floorLayer.value == 0 ? ~0 : floorLayer.value;

        if (Physics.Raycast(ray, out hit, clickMaxDistance, mask, QueryTriggerInteraction.Collide))
            return true;

        RaycastHit[] hits = Physics.RaycastAll(ray, clickMaxDistance, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null)
                continue;

            bool isFloorLayer = floorLayer.value != 0 && ((floorLayer.value & (1 << h.collider.gameObject.layer)) != 0);
            bool hasFloorTile = h.collider.GetComponentInParent<FloorTile>() != null;
            if (isFloorLayer || hasFloorTile)
            {
                hit = h;
                return true;
            }
        }

        return false;
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            ValidateRuntimeInvariants();

            if (ShouldAbortMovementExecution())
            {
                if (stepQueue.Count > 0)
                    stepQueue.Clear();
                movementState = MovementState.Idle;
                SetMovementAnimation(false);
                yield return null;
                continue;
            }

            if (movementState == MovementState.Idle && stepQueue.Count > 0)
            {
                movementState = MovementState.Moving;
                SetMovementAnimation(true);

                while (stepQueue.Count > 0)
                {
                    currentStepToken++;
                    targetGridPos = stepQueue.Dequeue();
                    yield return ExecuteStep(targetGridPos);
                    if (ShouldAbortMovementExecution())
                        break;
                    CompleteStep(targetGridPos, currentStepToken);
                }

                movementState = MovementState.Idle;
                SetMovementAnimation(false);
                if (stepQueue.Count != 0)
                    Debug.LogError("[MG2] Returned to Idle with non-empty stepQueue", this);
                ValidateRuntimeInvariants();
            }

            yield return null;
        }
    }

    private IEnumerator ExecuteStep(Vector2Int step)
    {
        Vector3 worldTarget = GetStepWorldTarget(step);

        while (true)
        {
            if (ShouldAbortMovementExecution())
                yield break;

            Vector3 flatDelta = worldTarget - moveRoot.position;
            flatDelta.y = 0f;
            if (flatDelta.magnitude <= arriveDistance)
                break;

            UpdateFacing(flatDelta);

            if (moverCharacterController != null)
            {
                Vector3 moveDelta = flatDelta.normalized * (moveSpeed * Time.deltaTime);
                if (moveDelta.magnitude > flatDelta.magnitude)
                    moveDelta = flatDelta;
                moveDelta.y = Physics.gravity.y * Time.deltaTime;
                moverCharacterController.Move(moveDelta);
            }
            else
            {
                moveRoot.position = Vector3.MoveTowards(moveRoot.position, worldTarget, moveSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    private void UpdateFacing(Vector3 flatDirection)
    {
        if (!rotateTowardsMovement)
            return;

        if (flatDirection.sqrMagnitude <= 0.0001f)
            return;

        Transform target = RotationTarget;
        if (target == null)
            return;

        Quaternion desired = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        target.rotation = Quaternion.RotateTowards(target.rotation, desired, rotationSpeedDegrees * Time.deltaTime);
    }

    private void CompleteStep(Vector2Int step, int stepToken)
    {
        if (stepToken <= lastCompletedStepToken)
        {
            Debug.LogError($"[MG2] Duplicate CompleteStep detected for token={stepToken} lastCompleted={lastCompletedStepToken}", this);
            return;
        }

        SnapVisualToGrid(step);
        targetGridPos = step;
        currentGridPos = targetGridPos;

        if (currentGridPos != targetGridPos)
        {
            Debug.LogError($"[MG2] Step completion invariant failed: current={currentGridPos} target={targetGridPos}", this);
        }

        (float stepCost, bool isSavingStep) = GetStepEnergyFromGrid(currentGridPos);
        if (recordStepsDirectly && miniGame2Manager != null)
            miniGame2Manager.RecordTileVisit(currentGridPos, stepCost, isSavingStep);

        totalExecutedSteps++;
        totalAppliedEnergy += Mathf.Max(0f, stepCost);
        GridManager.Node node = gridManager != null ? gridManager.GetNode(currentGridPos.x, currentGridPos.y) : null;
        if (node != null)
            totalExpectedEnergyFromGrid += Mathf.Max(0f, node.movementCost);
        visitedStepCoords.Add(currentGridPos);
        lastCompletedStepToken = stepToken;

        if (!Mathf.Approximately(totalAppliedEnergy, totalExpectedEnergyFromGrid))
        {
            Debug.LogError($"[MG2] Energy mismatch: applied={totalAppliedEnergy:F3} expected={totalExpectedEnergyFromGrid:F3}", this);
        }

        if (verboseMovementLogs)
        {
            Debug.Log($"[MG2] Step {totalExecutedSteps} complete at {currentGridPos.x},{currentGridPos.y} cost={stepCost:F2} totalEnergy={totalAppliedEnergy:F2}", this);
        }

        StepCompleted?.Invoke(currentGridPos, stepCost);

        miniGame2Manager?.NotifyRobotStepProcessed(currentGridPos);
    }

    private Vector3 GetStepWorldTarget(Vector2Int step)
    {
        if (gridManager == null)
            return moveRoot.position;

        Vector3 worldTarget = gridManager.GridToWorld(step.x, step.y);
        worldTarget.y = moveRoot.position.y;
        return worldTarget;
    }

    private void SnapVisualToGrid(Vector2Int step)
    {
        Vector3 worldTarget = GetStepWorldTarget(step);

        if (moverCharacterController != null)
        {
            bool wasEnabled = moverCharacterController.enabled;
            if (wasEnabled) moverCharacterController.enabled = false;
            moveRoot.position = worldTarget;
            if (wasEnabled) moverCharacterController.enabled = true;
        }
        else
        {
            moveRoot.position = worldTarget;
        }
    }

    private (float cost, bool isSaving) GetStepEnergyFromGrid(Vector2Int step)
    {
        if (gridManager == null)
            return (0f, false);

        GridManager.Node node = gridManager.GetNode(step.x, step.y);
        if (node == null)
            return (0f, false);

        return (node.movementCost, node.isEnergySaving);
    }

    private void ValidateRuntimeInvariants()
    {
        if (movementState == MovementState.Idle && stepQueue.Count > 0 && processRoutine == null)
            Debug.LogError("[MG2] Idle with pending steps but no processing coroutine", this);

        if (movementState == MovementState.Idle && stepQueue.Count == 0 && hasCurrentGridPos)
        {
            Vector3 center = GetStepWorldTarget(currentGridPos);
            float flatDistance = Vector2.Distance(new Vector2(moveRoot.position.x, moveRoot.position.z), new Vector2(center.x, center.z));
            if (flatDistance > Mathf.Max(arriveDistance, 0.1f))
                Debug.LogWarning($"[MG2] Visual drift detected: {flatDistance:F3} from grid center {currentGridPos}", this);
        }

        if (movementState == MovementState.Moving && !hasCurrentGridPos)
            Debug.LogError("[MG2] Moving state without initialized currentGridPos", this);
    }

    private bool ShouldAbortMovementExecution()
    {
        return miniGame2Manager != null && miniGame2Manager.CurrentPhase == MiniGame2Phase.Completed;
    }
}
