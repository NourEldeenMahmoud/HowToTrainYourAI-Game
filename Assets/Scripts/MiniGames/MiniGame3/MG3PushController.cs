using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MG3PushController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MG3GridManager gridManager;
    [SerializeField] private MG3RobotGridMover robotMover;
    [SerializeField] private InputActionReference interactAction;

    [Header("Push")]
    [SerializeField, Min(0.01f)] private float pushDuration = 0.2f;
    [SerializeField] private bool useWorldAdjacencyFallback = false;
    [SerializeField, Min(0.1f)] private float worldAdjacencyMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float prePushTurnDelay = 0.06f;
    [SerializeField, Min(0f)] private float minPushAnimationHold = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private bool suppressRepeatedRejectLogs = true;
    [SerializeField, Min(0f)] private float rejectLogCooldown = 0.25f;

    public event Action<MG3PushableDevice, Vector2Int, Vector2Int> PushCompleted;
    public event Action<MG3PushableDevice, Vector2Int, Vector2Int> PushStarted;
    public event Action<string> PushRejected;

    public bool IsPushing { get; private set; }

    private Coroutine pushRoutine;
    private string lastRejectReason;
    private float lastRejectTime;

    private static readonly Vector2Int[] Neighbors4 =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<MG3GridManager>();
        }

        if (robotMover == null)
        {
            robotMover = FindFirstObjectByType<MG3RobotGridMover>();
        }
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed += OnInteractPerformed;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
        }

        if (pushRoutine != null)
        {
            StopCoroutine(pushRoutine);
            pushRoutine = null;
        }

        IsPushing = false;
        if (robotMover != null)
        {
            robotMover.SetMovementLock(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPushFromCurrentPosition();
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryPushFromCurrentPosition();
    }

    public bool HasValidPushCandidate()
    {
        if (gridManager == null || robotMover == null || IsPushing || robotMover.IsMoving)
        {
            return false;
        }

        // Do NOT sync occupancy here; rely on gridManager's authoritative state
        return TryGetPushSetup(robotMover.CurrentGridCoord, out _, out _, out _, out _);
    }

    public bool TryPushFromCurrentPosition()
    {
        if (gridManager == null || robotMover == null)
        {
            EmitReject("Missing references", true);
            return false;
        }

        if (IsPushing || robotMover.IsMoving)
        {
            EmitReject("Robot is busy");
            return false;
        }

        // Do NOT sync occupancy here � the grid manager already has the correct state
        if (!TryGetPushSetup(robotMover.CurrentGridCoord, out MG3PushableDevice device, out Vector2Int fromCell, out Vector2Int toCell, out string failReason))
        {
            EmitReject(failReason);
            return false;
        }

        if (verboseLogs)
        {
            Debug.Log($"[MG3PushController] Push start: '{device.name}' {fromCell} -> {toCell}", this);
        }

        PushStarted?.Invoke(device, fromCell, toCell);

        pushRoutine = StartCoroutine(PushRoutine(device, fromCell, toCell));
        return true;
    }

    private bool TryGetPushSetup(Vector2Int robotCell, out MG3PushableDevice device, out Vector2Int fromCell, out Vector2Int toCell, out string failReason)
    {
        device = null;
        fromCell = default;
        toCell = default;
        failReason = "No pushable device behind E interaction";

        int foundCount = 0;
        for (int i = 0; i < Neighbors4.Length; i++)
        {
            Vector2Int candidateCell = robotCell + Neighbors4[i];
            if (!gridManager.TryGetOccupantKind(candidateCell, out MG3GridManager.OccupantKind kind))
            {
                continue;
            }

            if (kind != MG3GridManager.OccupantKind.Pushable)
            {
                continue;
            }

            MG3PushableDevice candidate = FindPushableAtCell(candidateCell);
            if (candidate == null || candidate.IsLocked)
            {
                continue;
            }

            foundCount++;
            device = candidate;
            fromCell = candidateCell;
            toCell = candidateCell + Neighbors4[i];
        }

        if (foundCount == 0 || device == null)
        {
            if (useWorldAdjacencyFallback && TryGetPushSetupFromWorld(robotCell, out device, out fromCell, out toCell))
            {
                return true;
            }

            failReason = "No adjacent pushable device";
            return false;
        }

        if (foundCount > 1)
        {
            failReason = "Ambiguous push: multiple adjacent pushables";
            return false;
        }

        if (!gridManager.IsInBounds(toCell))
        {
            failReason = "Push target out of bounds";
            return false;
        }

        if (gridManager.IsCellOccupied(toCell))
        {
            failReason = "Push target occupied";
            return false;
        }

        if (!gridManager.TryGetTile(toCell, out MG3GridTile tile) || !tile.Walkable)
        {
            failReason = "Push target blocked";
            return false;
        }

        return true;
    }

    private bool TryGetPushSetupFromWorld(Vector2Int robotCell, out MG3PushableDevice device, out Vector2Int fromCell, out Vector2Int toCell)
    {
        device = null;
        fromCell = default;
        toCell = default;

        MG3PushableDevice[] devices = FindObjectsByType<MG3PushableDevice>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (devices.Length == 0)
        {
            return false;
        }

        float cell = gridManager != null ? Mathf.Max(0.1f, gridManager.CellSize) : 1f;
        float maxDistance = cell * worldAdjacencyMultiplier;
        float bestScore = float.PositiveInfinity;
        MG3PushableDevice best = null;
        Vector2Int bestDir = default;

        Vector3 robotPos = robotMover.transform.position;
        for (int i = 0; i < devices.Length; i++)
        {
            MG3PushableDevice d = devices[i];
            if (d == null || d.IsLocked)
            {
                continue;
            }

            Vector3 delta = d.transform.position - robotPos;
            Vector2 planar = new Vector2(delta.x, delta.z);
            float dist = planar.magnitude;
            if (dist < 0.01f || dist > maxDistance)
            {
                continue;
            }

            Vector2Int dir;
            if (Mathf.Abs(planar.x) >= Mathf.Abs(planar.y))
            {
                dir = new Vector2Int(planar.x >= 0f ? 1 : -1, 0);
            }
            else
            {
                dir = new Vector2Int(0, planar.y >= 0f ? 1 : -1);
            }

            Vector2Int dCell = gridManager.WorldToGrid(d.transform.position);
            Vector2Int expectedCell = robotCell + dir;
            if (dCell != expectedCell)
            {
                continue;
            }

            float score = Vector2Int.Distance(dCell, expectedCell) + Mathf.Abs(dist - cell);
            if (score < bestScore)
            {
                bestScore = score;
                best = d;
                bestDir = dir;
            }
        }

        if (best == null)
        {
            return false;
        }

        fromCell = gridManager.WorldToGrid(best.transform.position);
        if (!gridManager.IsInBounds(fromCell) && !gridManager.TryFindNearestTileCoord(best.transform.position, out fromCell))
        {
            return false;
        }

        toCell = fromCell + bestDir;
        device = best;

        if (!gridManager.IsInBounds(toCell) || gridManager.IsCellOccupied(toCell))
        {
            return false;
        }

        if (!gridManager.TryGetTile(toCell, out MG3GridTile tile) || !tile.Walkable)
        {
            return false;
        }

        if (verboseLogs)
        {
            Debug.Log($"[MG3PushController] Using world-adjacency fallback for '{best.name}' from {fromCell} to {toCell}", this);
        }

        return true;
    }

    private MG3PushableDevice FindPushableAtCell(Vector2Int cell)
    {
        // First, trust the grid manager's occupancy handle
        if (gridManager != null && gridManager.TryGetOccupantHandle(cell, out UnityEngine.Object handle))
        {
            MG3PushableDevice device = handle as MG3PushableDevice;
            if (device != null && !device.IsLocked)
            {
                return device;
            }
        }

        return null;
    }

    private IEnumerator PushRoutine(MG3PushableDevice device, Vector2Int fromCell, Vector2Int toCell)
    {
        IsPushing = true;
        robotMover.SetMovementLock(true);
        Vector3 pushDirection = gridManager.GridToWorld(toCell) - gridManager.GridToWorld(fromCell);
        robotMover.FaceDirection(pushDirection);
        robotMover.SetPushingAnimation(true);
        robotMover.DebugLogAnimatorSnapshot("PushStart-BeforeDelay");
        if (prePushTurnDelay > 0f)
        {
            yield return new WaitForSeconds(prePushTurnDelay);
        }
        robotMover.PlayPushAnimation();
        robotMover.DebugLogAnimatorSnapshot("PushStart-AfterTrigger");

        if (!gridManager.MoveOccupant(device, toCell))
        {
            string reason = "Push target became occupied";
            if (verboseLogs)
            {
                if (!gridManager.IsInBounds(toCell))
                {
                    reason = "Push target out of bounds";
                    Debug.LogWarning($"[MG3PushController] Push cancelled: target {toCell} is out of bounds.", this);
                }
                else if (!gridManager.TryGetOccupantHandle(fromCell, out UnityEngine.Object atFrom) || atFrom != device)
                {
                    reason = "Push source no longer valid";
                    string occupantName = atFrom != null ? atFrom.name : "null";
                    Debug.LogWarning($"[MG3PushController] Push cancelled: source {fromCell} no longer mapped to '{device.name}' (current='{occupantName}').", this);
                }
                else if (gridManager.TryGetOccupantDebug(toCell, out MG3GridManager.OccupantKind kind, out string handleName))
                {
                    Debug.LogWarning($"[MG3PushController] Push cancelled: target {toCell} occupied by kind={kind} handle='{handleName}'.", this);
                }
                else
                {
                    Debug.LogWarning($"[MG3PushController] Push cancelled: target {toCell} became occupied.", this);
                }
            }
            EmitReject(reason, true);
            robotMover.SetPushingAnimation(false);
            robotMover.SetMovementLock(false);
            IsPushing = false;
            yield break;
        }

        Vector3 startPos = device.transform.position;
        Vector3 gridEndPos = device.GetWorldPositionForCoordinate(toCell);
        Vector3 endPos = new Vector3(gridEndPos.x, startPos.y, gridEndPos.z);
        Vector3 robotStartPos = robotMover.transform.position;
        Vector3 gridRobotEnd = gridManager.GridToWorld(fromCell);
        Vector3 robotEndPos = new Vector3(gridRobotEnd.x, robotMover.transform.position.y, gridRobotEnd.z);
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, pushDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            device.transform.position = Vector3.Lerp(startPos, endPos, t);
            robotMover.transform.position = Vector3.Lerp(robotStartPos, robotEndPos, t);
            yield return null;
        }

        float remainingHold = minPushAnimationHold - elapsed;
        if (remainingHold > 0f)
        {
            yield return new WaitForSeconds(remainingHold);
        }

        robotMover.DebugLogAnimatorSnapshot("PushMid-BeforeRelease");

        device.transform.position = endPos;
        device.SetCurrentCoordinate(toCell, false);
        robotMover.WarpToCoordinate(fromCell);

        IsPushing = false;
        pushRoutine = null;
        robotMover.SetPushingAnimation(false);
        robotMover.DebugLogAnimatorSnapshot("PushEnd-AfterRelease");
        robotMover.SetMovementLock(false);
        if (verboseLogs)
        {
            Debug.Log($"[MG3PushController] Push complete: '{device.name}' now at {toCell}", this);
        }
        PushCompleted?.Invoke(device, fromCell, toCell);
    }

    private void EmitReject(string reason, bool warning = false)
    {
        if (suppressRepeatedRejectLogs && reason == lastRejectReason && (Time.unscaledTime - lastRejectTime) < rejectLogCooldown)
        {
            PushRejected?.Invoke(reason);
            return;
        }

        lastRejectReason = reason;
        lastRejectTime = Time.unscaledTime;

        if (verboseLogs)
        {
            if (warning) Debug.LogWarning($"[MG3PushController] Push rejected: {reason}", this);
            else Debug.Log($"[MG3PushController] Push rejected: {reason}", this);
        }

        PushRejected?.Invoke(reason);
    }
}
