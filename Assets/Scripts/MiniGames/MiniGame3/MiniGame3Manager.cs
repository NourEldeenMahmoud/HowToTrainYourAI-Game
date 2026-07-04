using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MiniGame3Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MG3GridManager gridManager;
    [SerializeField] private MG3RobotGridMover robotMover;
    [SerializeField] private MG3PushController pushController;
    [SerializeField] private MG3TaskValidator taskValidator;

    [Header("Task Flow")]
    [SerializeField] private MG3TaskDefinition[] tasks;
    [SerializeField] private bool autoStartOnSceneLoad = false;
    [SerializeField] private bool useTaskRobotStartCoordinate = false;
    [SerializeField, Min(0.01f)] private float settleDelaySeconds = 0.2f;
    [SerializeField, Min(0.01f)] private float completionPopupSeconds = 0.8f;
    [SerializeField, Min(0.01f)] private float deadlockPopupSeconds = 0.9f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    public event Action<MiniGame3Phase> PhaseChanged;
    public event Action<MG3TaskDefinition, int, int> TaskStarted;
    public event Action<MG3TaskDefinition, int, int> TaskCompleted;
    public event Action<MG3TaskDefinition> TaskReset;
    public event Action<string> Feedback;
    public event Action MiniGameCompleted;
    public event Action<int, int> StatsChanged;

    public MiniGame3Phase CurrentPhase { get; private set; } = MiniGame3Phase.Idle;
    public int CurrentTaskIndex { get; private set; } = -1;
    public int TotalPushes { get; private set; }
    public int TotalResets { get; private set; }

    private Coroutine settleRoutine;
    private MG3PushableDevice lastPushedDevice;

    private static readonly Vector2Int[] Neighbors4 =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private MG3TaskDefinition CurrentTask => (CurrentTaskIndex >= 0 && CurrentTaskIndex < tasks.Length) ? tasks[CurrentTaskIndex] : null;

    private void Awake()
    {
        MiniGame3Manager[] managers = FindObjectsByType<MiniGame3Manager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length > 1)
        {
            var names = new List<string>(managers.Length);
            for (int i = 0; i < managers.Length; i++)
            {
                names.Add($"{managers[i].name}#{managers[i].GetEntityId()}");
            }

            Debug.LogWarning($"[MiniGame3Manager] Multiple manager instances found ({managers.Length}): {string.Join(", ", names)}", this);
        }

        if (gridManager == null) gridManager = FindFirstObjectByType<MG3GridManager>();
        if (robotMover == null) robotMover = FindFirstObjectByType<MG3RobotGridMover>();
        if (pushController == null) pushController = FindFirstObjectByType<MG3PushController>();
        if (taskValidator == null) taskValidator = FindFirstObjectByType<MG3TaskValidator>();
    }

    private void OnEnable()
    {
        if (pushController != null)
        {
            pushController.PushStarted += OnPushStarted;
            pushController.PushCompleted += OnPushCompleted;
            pushController.PushRejected += OnPushRejected;
        }

        if (robotMover != null)
        {
            robotMover.DestinationRequested += OnDestinationRequested;
            robotMover.MovementStarted += OnMovementStarted;
            robotMover.DestinationRejected += OnDestinationRejected;
            robotMover.DestinationReached += OnDestinationReached;
        }
    }

    private void OnDisable()
    {
        if (pushController != null)
        {
            pushController.PushStarted -= OnPushStarted;
            pushController.PushCompleted -= OnPushCompleted;
            pushController.PushRejected -= OnPushRejected;
        }

        if (robotMover != null)
        {
            robotMover.DestinationRequested -= OnDestinationRequested;
            robotMover.MovementStarted -= OnMovementStarted;
            robotMover.DestinationRejected -= OnDestinationRejected;
            robotMover.DestinationReached -= OnDestinationReached;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SkipCurrentTask();
        }
    }

    private void Start()
    {
        if (verboseLogs)
        {
            Debug.Log($"[MiniGame3Manager] Start on '{name}'#{GetEntityId()} autoStartOnSceneLoad={autoStartOnSceneLoad} useTaskRobotStartCoordinate={useTaskRobotStartCoordinate}", this);
        }

        SetPhase(MiniGame3Phase.Idle);
        EmitFeedback("MiniGame3 manager ready. Waiting for manual start.");
        if (autoStartOnSceneLoad)
        {
            StartMiniGame();
        }
    }

    [ContextMenu("Start MiniGame3")]
    public void StartMiniGame()
    {
        if (tasks == null || tasks.Length == 0)
        {
            EmitFeedback("No tasks configured");
            return;
        }

        TotalPushes = 0;
        TotalResets = 0;
        StatsChanged?.Invoke(TotalPushes, TotalResets);

        // Reset slot visuals for every task up-front so none appear pre-solved.
        for (int t = 0; t < tasks.Length; t++)
        {
            if (tasks[t] != null)
            {
                tasks[t].ResetSlotVisuals();
            }
        }

        StartTask(0);
    }

    public void SkipCurrentTask()
    {
        MG3TaskDefinition task = CurrentTask;
        if (task == null || CurrentPhase == MiniGame3Phase.Completed) return;

        MG3PushableDevice[] taskDevices = task.Devices;
        if (taskDevices != null)
        {
            for (int i = 0; i < taskDevices.Length; i++)
            {
                if (taskDevices[i] != null)
                    taskDevices[i].SetLocked(true);
            }
        }

        MG3TargetSlot[] slots = task.Slots;
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].SetSolved(true);
            }
        }

        EmitFeedback($"Skipped: {task.TaskName}");
        TaskCompleted?.Invoke(task, CurrentTaskIndex, tasks.Length);
        StartCoroutine(SkipAndAdvance());
    }

    private IEnumerator SkipAndAdvance()
    {
        yield return new WaitForSeconds(completionPopupSeconds);
        StartTask(CurrentTaskIndex + 1);
    }

    [ContextMenu("Reset Current Task")]
    public void ResetCurrentTask()
    {
        MG3TaskDefinition task = CurrentTask;
        if (task == null)
        {
            EmitFeedback("No active task to reset");
            return;
        }

        SetPhase(MiniGame3Phase.Resetting);
        TotalResets++;
        StatsChanged?.Invoke(TotalPushes, TotalResets);
        ResetTaskState(task, repositionRobot: useTaskRobotStartCoordinate);
        TaskReset?.Invoke(task);
        EmitFeedback($"Task reset: {task.TaskName}");
        SetPhase(MiniGame3Phase.WaitingForInput);
    }

    public void StartTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= tasks.Length)
        {
            SetPhase(MiniGame3Phase.Completed);
            MiniGameCompleted?.Invoke();
            EmitFeedback("All tasks complete.");
            return;
        }

        CurrentTaskIndex = taskIndex;
        MG3TaskDefinition task = tasks[taskIndex];
        if (task == null)
        {
            EmitFeedback($"Task {taskIndex + 1} missing");
            return;
        }

        SetPhase(MiniGame3Phase.TaskIntro);
        ResetTaskState(task, repositionRobot: useTaskRobotStartCoordinate);
        task.ResetSlotVisuals();
        TaskStarted?.Invoke(task, taskIndex, tasks.Length);
        EmitFeedback($"Task {taskIndex + 1}/{tasks.Length}: {task.TaskName}");
        SetPhase(MiniGame3Phase.WaitingForInput);

        // Immediately evaluate the task in case designer-placed devices already satisfy the task.
        if (taskValidator != null)
        {
            StartCoroutine(EvaluateTaskAtStart(task));
        }
    }

    private IEnumerator EvaluateTaskAtStart(MG3TaskDefinition task)
    {
        // small delay to ensure any registrations/syncs completed
        yield return null;

        if (task == null || taskValidator == null)
        {
            yield break;
        }

        // If already solved, follow same completion flow as after a push
        if (taskValidator.ValidateTask(task))
        {
            taskValidator.LockSolvedDevices(task);
            TaskCompleted?.Invoke(task, CurrentTaskIndex, tasks.Length);
            EmitFeedback($"Task complete: {task.TaskName}");
            yield return new WaitForSeconds(completionPopupSeconds);
            StartTask(CurrentTaskIndex + 1);
        }
    }

    private void ResetTaskState(MG3TaskDefinition task, bool repositionRobot)
    {
        if (task == null)
        {
            return;
        }

        if (gridManager != null)
        {
            gridManager.ClearAllRuntimeOccupancy();
        }

        // Task-scoped reset: ONLY reset devices belonging to the currently active task.
        // Devices from previous tasks must remain untouched and permanent.
        MG3PushableDevice[] taskDevices = task.Devices;
        if (taskDevices != null)
        {
            for (int i = 0; i < taskDevices.Length; i++)
            {
                MG3PushableDevice dev = taskDevices[i];
                if (dev == null) continue;

                // Skip devices locked by a previously completed task (or correctly placed)
                if (dev.IsLocked) continue;

                dev.ResetToTaskStart();
            }
        }

        // ── Re-register locked devices from ALL tasks ─────────────────────────
        // ClearAllRuntimeOccupancy() above wiped every occupant, including devices
        // that were already locked by previously completed tasks. Locked devices
        // skip ResetToTaskStart() (correct — they must not move), so we must
        // manually restore their grid occupancy here so they continue to block
        // movement and pathfinding as solid LockedPushable obstacles.
        if (gridManager != null && tasks != null)
        {
            for (int t = 0; t < tasks.Length; t++)
            {
                MG3TaskDefinition otherTask = tasks[t];
                if (otherTask == null) continue;

                MG3PushableDevice[] otherDevices = otherTask.Devices;
                if (otherDevices == null) continue;

                for (int d = 0; d < otherDevices.Length; d++)
                {
                    MG3PushableDevice dev = otherDevices[d];
                    if (dev == null || !dev.IsLocked) continue;

                    // Only register if the cell is not already claimed (another
                    // task may reference the same device object; guard duplicates).
                    if (!gridManager.IsCellOccupied(dev.CurrentCoordinate))
                    {
                        gridManager.RegisterOccupant(dev, dev.CurrentCoordinate, MG3GridManager.OccupantKind.LockedPushable);
                        if (verboseLogs)
                        {
                            Debug.Log($"[MiniGame3Manager] Restored locked occupancy for '{dev.name}' at {dev.CurrentCoordinate} after reset.", this);
                        }
                    }
                }
            }

            RegisterFutureTaskDevicesAsBlockers();
        }

        LogTask3DeviceAlignment(task);

        if (robotMover != null)
        {
            if (repositionRobot)
            {
                robotMover.WarpToCoordinate(task.RobotStartCoordinate);
            }
            else if (gridManager != null)
            {
                Vector2Int current = gridManager.WorldToGrid(robotMover.transform.position);
                if (!gridManager.IsInBounds(current) && gridManager.TryFindNearestTileCoord(robotMover.transform.position, out Vector2Int nearest))
                {
                    current = nearest;
                }

                // If the robot's cell is now occupied by a freshly-reset device, find the
                // nearest free walkable cell so WarpToCoordinate registers successfully.
                if (gridManager.IsCellOccupied(current))
                {
                    if (gridManager.TryFindNearestFreeWalkableCoord(robotMover.transform.position, out Vector2Int freeCell))
                    {
                        current = freeCell;
                    }
                }

                robotMover.WarpToCoordinate(current);
            }

            robotMover.SetMovementLock(false);
        }
    }

    private void RegisterFutureTaskDevicesAsBlockers()
    {
        if (gridManager == null || tasks == null)
        {
            return;
        }

        for (int t = CurrentTaskIndex + 1; t < tasks.Length; t++)
        {
            MG3TaskDefinition futureTask = tasks[t];
            if (futureTask == null) continue;

            MG3PushableDevice[] futureDevices = futureTask.Devices;
            if (futureDevices == null) continue;

            for (int d = 0; d < futureDevices.Length; d++)
            {
                MG3PushableDevice dev = futureDevices[d];
                if (dev == null || dev.IsLocked) continue;

                Vector2Int coord = dev.CurrentCoordinate;
                if (!gridManager.IsInBounds(coord)) continue;

                if (gridManager.IsCellOccupied(coord)) continue;

                gridManager.RegisterOccupant(dev, coord, MG3GridManager.OccupantKind.LockedPushable);
            }
        }
    }

    private void LogTask3DeviceAlignment(MG3TaskDefinition task)
    {
        if (gridManager == null || task == null || task.TaskType != MG3TaskType.SizeOrdering)
        {
            return;
        }

        MG3PushableDevice[] taskDevices = task.Devices;
        if (taskDevices == null)
        {
            return;
        }

        for (int i = 0; i < taskDevices.Length; i++)
        {
            MG3PushableDevice device = taskDevices[i];
            if (device == null)
            {
                continue;
            }

            Vector3 actualPosition = device.transform.position;
            Vector3 gridCenter = gridManager.GridToWorld(device.CurrentCoordinate);
            Vector3 expectedPosition = device.GetWorldPositionForCoordinate(device.CurrentCoordinate);
            float planarDistance = Vector2.Distance(
                new Vector2(actualPosition.x, actualPosition.z),
                new Vector2(expectedPosition.x, expectedPosition.z));

            Debug.Log(
                $"[MiniGame3Manager][Task3 Alignment] '{device.name}' " +
                $"current={device.CurrentCoordinate} start={device.StartingCoordinate} " +
                $"actual={actualPosition} expectedVisual={expectedPosition} gridCenter={gridCenter} " +
                $"visualDelta={planarDistance:0.###}",
                device);
        }
    }

    private void OnDestinationRejected(Vector2Int destination, string reason)
    {
        EmitFeedback($"Unreachable {destination}: {reason}");
    }

    private void OnDestinationRequested(Vector2Int destination)
    {
        SetPhase(MiniGame3Phase.Pathfinding);
        EmitFeedback($"Pathfinding to {destination}...");
    }

    private void OnMovementStarted(Vector2Int from, Vector2Int to)
    {
        SetPhase(MiniGame3Phase.Moving);
        EmitFeedback($"Moving {from} -> {to}");
    }

    private void OnDestinationReached(Vector2Int destination)
    {
        SetPhase(MiniGame3Phase.ReadyToPush);
        EmitFeedback($"Reached {destination}. Press E to push.");
        SetPhase(MiniGame3Phase.WaitingForInput);
    }

    private void OnPushRejected(string reason)
    {
        EmitFeedback($"Push rejected: {reason}");
    }

    private void OnPushStarted(MG3PushableDevice device, Vector2Int fromCell, Vector2Int toCell)
    {
        SetPhase(MiniGame3Phase.Pushing);
        EmitFeedback($"Pushing {device.name}: {fromCell} -> {toCell}");
    }

    private void OnPushCompleted(MG3PushableDevice device, Vector2Int fromCell, Vector2Int toCell)
    {
        TotalPushes++;
        StatsChanged?.Invoke(TotalPushes, TotalResets);
        lastPushedDevice = device;
        EmitFeedback($"Push complete: {device.name} {fromCell} -> {toCell}");
        if (settleRoutine != null)
        {
            StopCoroutine(settleRoutine);
        }

        settleRoutine = StartCoroutine(SettleAndEvaluate());
    }

    private IEnumerator SettleAndEvaluate()
    {
        SetPhase(MiniGame3Phase.Settling);
        yield return new WaitForSeconds(settleDelaySeconds);

        SetPhase(MiniGame3Phase.CheckingCompletion);
        
        MG3TaskDefinition task = CurrentTask;
        if (task == null || taskValidator == null)
        {
            SetPhase(MiniGame3Phase.WaitingForInput);
            yield break;
        }

        bool solved = taskValidator.ValidateTask(task);

        // Standard task-based lock
        taskValidator.LockSolvedDevices(task);

        // Bulletproof aggressive lock for the specific device we just pushed.
        // This ensures that even if task mapping/dictionaries fail, if the device is on
        // a correctly solved slot, it gets locked permanently.
        if (lastPushedDevice != null && !lastPushedDevice.IsLocked)
        {
            for (int i = 0; i < task.Slots.Length; i++)
            {
                MG3TargetSlot slot = task.Slots[i];
                if (slot != null && slot.Coordinate == lastPushedDevice.CurrentCoordinate)
                {
                    bool isCorrect = false;
                    if (task.TaskType == MG3TaskType.ExactPlacement && !string.IsNullOrEmpty(slot.RequiredDeviceId))
                    {
                        isCorrect = lastPushedDevice.DeviceId == slot.RequiredDeviceId;
                    }
                    else if (task.TaskType == MG3TaskType.GroupPlacement && !string.IsNullOrEmpty(slot.RequiredGroupId))
                    {
                        isCorrect = lastPushedDevice.GroupId == slot.RequiredGroupId;
                    }
                    else if (task.TaskType == MG3TaskType.SizeOrdering)
                    {
                        isCorrect = lastPushedDevice.SizeRank == slot.RequiredSizeRank;
                    }

                    if (isCorrect)
                    {
                        lastPushedDevice.SetLocked(true);
                        slot.SetSolved(true); // Ensure visual state is updated
                        if (verboseLogs) Debug.Log($"[MiniGame3Manager] Force-locked {lastPushedDevice.name} at {slot.Coordinate}", this);
                    }
                    break;
                }
            }
        }

        // If the pushed device was NOT correctly placed, check if it reached a deadlock.
        if (lastPushedDevice != null && !lastPushedDevice.IsLocked && IsDeviceDeadlocked(lastPushedDevice))
        {
            EmitFeedback($"Deadlock detected: {lastPushedDevice.name}");
            yield return new WaitForSeconds(deadlockPopupSeconds);
            ResetCurrentTask();
            yield break;
        }

        if (!solved)
        {
            SetPhase(MiniGame3Phase.WaitingForInput);
            yield break;
        }

        taskValidator.LockSolvedDevices(task);
        if (verboseLogs) Debug.Log($"[MiniGame3Manager] Invoking TaskCompleted for index={CurrentTaskIndex} task='{task.TaskName}'", this);
        TaskCompleted?.Invoke(task, CurrentTaskIndex, tasks.Length);
        EmitFeedback($"Task complete: {task.TaskName}");
        yield return new WaitForSeconds(completionPopupSeconds);
        StartTask(CurrentTaskIndex + 1);
    }

    private bool IsDeviceDeadlocked(MG3PushableDevice device)
    {
        if (device == null || gridManager == null)
        {
            return false;
        }

        Vector2Int objectCell = device.CurrentCoordinate;
        // The robot's current cell is never a permanent obstacle — it will move.
        // Exclude it from both setup and target occupancy checks so a fresh push
        // never creates a false deadlock in the direction the robot just came from.
        Vector2Int robotCell = robotMover != null ? robotMover.CurrentGridCoord : new Vector2Int(int.MinValue, int.MinValue);

        for (int i = 0; i < Neighbors4.Length; i++)
        {
            Vector2Int dir = Neighbors4[i];
            Vector2Int setupCell = objectCell - dir;
            Vector2Int targetCell = objectCell + dir;

            bool setupOccupied = gridManager.IsCellOccupied(setupCell) && setupCell != robotCell;
            bool targetOccupied = gridManager.IsCellOccupied(targetCell) && targetCell != robotCell;

            bool setupValid = gridManager.IsInBounds(setupCell) && gridManager.IsWalkable(setupCell) && !setupOccupied;
            bool targetValid = gridManager.IsInBounds(targetCell) && gridManager.IsWalkable(targetCell) && !targetOccupied;
            if (setupValid && targetValid)
            {
                return false;
            }
        }

        return true;
    }

    private void SetPhase(MiniGame3Phase phase)
    {
        CurrentPhase = phase;
        PhaseChanged?.Invoke(phase);
        if (verboseLogs)
        {
            Debug.Log($"[MiniGame3Manager] Phase -> {phase}", this);
        }
    }

    private void EmitFeedback(string message)
    {
        Feedback?.Invoke(message);
        if (verboseLogs)
        {
            Debug.Log($"[MiniGame3Manager] {message}", this);
        }
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        if (robotMover != null)
        {
            robotMover.SetMovementLock(!enabled);
        }

        if (pushController != null)
        {
            pushController.enabled = enabled;
        }
    }

    public void ReloadCurrentScene()
    {
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }
}
