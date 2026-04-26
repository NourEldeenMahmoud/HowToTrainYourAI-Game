using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame2Manager : MonoBehaviour
{
    [Header("Data Assets")]
    [SerializeField] private MiniGame2LearningProfileSO learningProfile;
    [SerializeField] private RobotStatsSO robotStats;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private TileClickMover tileClickMover;

    [Header("Route Anchors")]
    [Tooltip("Optional. If null, start coordinate is inferred from robot position at Start().")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform audioCardTarget;
    [SerializeField] private Transform robot;

    [Header("Audio Card Interaction")]
    [Tooltip("Max tile distance for interacting with the audio card. 0 = same tile only, 1 = adjacent allowed.")]
    [SerializeField, Range(0, 2)] private int cardInteractRangeTiles = 1;
    [Tooltip("If true, diagonal adjacent tiles count as in range.")]
    [SerializeField] private bool allowDiagonalCardInteraction = true;

    [Header("Logging / Feedback")]
    [SerializeField] private bool enableLogging = true;
    [Tooltip("If enabled, scoring/evaluation details are always printed to Console for debugging even before UI is finalized.")]
    [SerializeField] private bool debugScoringToConsole = true;
    [Tooltip("When actual energy exceeds idealEnergy * (1 + tolerance), emit 'Path inefficiency detected'.")]
    [SerializeField, Range(0f, 2f)] private float inefficiencyTolerance = 0.15f;
    [Tooltip("Emit 'Energy level critical' when actual energy exceeds this fraction of the ideal energy (after ideal is computed).")]
    [SerializeField, Range(0f, 5f)] private float energyCriticalAtIdealMultiplier = 1.25f;

    [Header("Energy Budget")]
    [SerializeField] private bool useEnergyBudget = true;
    [SerializeField, Min(0.01f)] private float startingEnergyBudget = 40f;

    [Header("Input (MiniGame2)")]
    [Tooltip("Disable RobotMovement (WASD) while MiniGame2 is active. Movement will be driven by TileClickMover only.")]
    [SerializeField] private bool disableRobotWasdMovement = true;
    [Tooltip("Force mouse cursor visible/unlocked while MiniGame2 is active so tile clicking remains usable.")]
    [SerializeField] private bool forceCursorVisibleForTileInput = true;

    [Header("Post-Result Return And Exit")]
    [SerializeField] private string returnToMainSceneName = "Nour";
    [Tooltip("Optional world-space target for the MG2 post-result return demo. If null, manager falls back to the MG2 start tile.")]
    [SerializeField] private Transform returnDemoEndPoint;
    [SerializeField] private bool useStartTileAsReturnFallback = true;
    [SerializeField] private string mainSceneGateAnchorName = "Warehouse_Door.B";
    [SerializeField] private string mainSceneGateFallbackAnchorName = "Warehouse_Door.F";
    [SerializeField] private Vector3 mainScenePlayerLocalOffset = new Vector3(-0.85f, 0f, -2.2f);
    [SerializeField] private Vector3 mainSceneRobotLocalOffset = new Vector3(0.75f, 0f, -1.35f);
    [SerializeField] private bool faceSpawnedActorsTowardGate = true;
    [SerializeField, Min(0.25f)] private float returnPathTimeoutSeconds = 12f;
    [SerializeField, Min(0.05f)] private float returnPathTimeoutSecondsPerStep = 0.75f;
    [SerializeField, Min(1f)] private float returnPathTimeoutSafetyMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float returnDemoNearestTileMaxDistance = 2.5f;
    [SerializeField, Min(0f)] private float holdAtGateBeforeTransitionSeconds = 0.75f;
    [SerializeField, Min(0.05f)] private float exitFadeDurationSeconds = 1f;
    [SerializeField] private bool enableReturnDemoLogs = true;

    [Header("Intro Camera Sequence")]
    [SerializeField] private bool playIntroCameraSequence = true;
    [SerializeField] private MG2CinemachineTopDownInput introTopDownInput;
    [Tooltip("Optional explicit start position for intro camera target movement.")]
    [SerializeField] private Transform introStartMarker;
    [Tooltip("Optional explicit focus position for intro camera target movement.")]
    [SerializeField] private Transform introFocusMarker;
    [SerializeField] private bool snapToIntroStartBeforeReveal = true;
    [Tooltip("Optional fallback camera pose if Cinemachine target is unavailable.")]
    [SerializeField] private Transform introStartCameraPose;
    [Tooltip("Optional fallback camera focus pose if Cinemachine target is unavailable.")]
    [SerializeField] private Transform introFocusCameraPose;
    [SerializeField, Min(0f)] private float introDelayBeforeMove = 0.35f;
    [SerializeField, Min(0.05f)] private float introMoveDuration = 1.4f;
    [SerializeField, Min(0f)] private float introHoldDuration = 0.7f;
    [SerializeField, Min(0f)] private float introDelayBeforeGameplay = 0.35f;
    [SerializeField] private bool lockInputDuringIntro = true;

    private MiniGame2Phase phase = MiniGame2Phase.Idle;
    private bool cardReached;
    private bool cardInteracted;
    private bool hasLoggedInefficiency;
    private bool hasLoggedEnergyCritical;

    private Vector2Int startCoord;
    private Vector2Int cardCoord;

    private float totalActualEnergy;
    private float remainingEnergy;
    private float idealEnergy;
    private List<Vector2Int> idealPath;
    private readonly List<Vector2Int> actualPath = new List<Vector2Int>(256);
    private Vector2Int? lastVisited;
    private bool miniGameEnded;
    private bool endedByEnergyDepletion;
    private bool introSequenceRunning;
    private bool isAudioCardInRange;
    private bool returnSequenceRunning;
    private Coroutine returnSequenceRoutine;

    public event Action<MiniGame2EvaluationResult> MiniGameCompleted;
    public event Action<MiniGame2Phase> PhaseChanged;
    public event Action<string> LogMessage;
    public event Action<bool> IntroSequenceStateChanged;
    public event Action<bool> AudioCardInteractRangeChanged;

    public MiniGame2Phase CurrentPhase => phase;
    public RobotStatsSO RobotStats => robotStats;
    public IReadOnlyList<Vector2Int> IdealPath => idealPath;
    public IReadOnlyList<Vector2Int> ActualPath => actualPath;
    public float IdealEnergy => idealEnergy;
    public float ActualEnergy => totalActualEnergy;
    public float RemainingEnergy => remainingEnergy;
    public float StartingEnergyBudget => startingEnergyBudget;
    public int ActualMoveCount => actualPath.Count;
    public bool IsMiniGameRunning => phase == MiniGame2Phase.Planning || phase == MiniGame2Phase.RobotMoving;
    public bool IsIntroSequenceRunning => introSequenceRunning;
    public bool IsAudioCardInRange => isAudioCardInRange;
    public bool IsReturnSequenceRunning => returnSequenceRunning;
    public MiniGame2EvaluationResult LastResult { get; private set; }
    public bool HasPassedLastRun
    {
        get
        {
            float pass = Mathf.Max(50f, learningProfile != null ? learningProfile.passScore : 50f);
            return LastResult.finalScore >= pass && LastResult.tier != MiniGameTier.Fail;
        }
    }

    public void StartReturnToGateAndExitSequence()
    {
        if (returnSequenceRunning)
            return;

        if (!LastResult.isSuccess)
        {
            if (enableReturnDemoLogs)
                Debug.LogWarning("[MG2] Return-to-gate sequence rejected: last result is not a success.", this);
            return;
        }

        if (!isActiveAndEnabled)
            return;

        returnSequenceRoutine = StartCoroutine(ReturnToGateAndExitRoutine());
    }

    public bool TryGetStartCoord(out Vector2Int coord)
    {
        if (gridManager == null)
        {
            coord = default;
            return false;
        }

        if (startPoint == null && robot == null)
        {
            if (tileClickMover == null) tileClickMover = FindFirstObjectByType<TileClickMover>();
            if (tileClickMover != null) robot = tileClickMover.MoverRoot;
        }

        if (startPoint == null && robot == null)
        {
            coord = default;
            return false;
        }

        ResolveRouteCoords();
        coord = startCoord;
        return true;
    }

    private IEnumerator Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (controlManager == null) controlManager = FindFirstObjectByType<ControlManager>();
        if (tileClickMover == null) tileClickMover = FindFirstObjectByType<TileClickMover>();
        if (robot == null && tileClickMover != null) robot = tileClickMover.transform;

        // Last-resort fallback: find the robot by tag.
        if (robot == null)
        {
            GameObject robotGo = GameObject.FindWithTag("Robot");
            if (robotGo != null)
            {
                robot = robotGo.transform;
                Debug.LogWarning("[MG2Manager] robot reference was null — resolved via Tag 'Robot'. Assign it directly in the Inspector for reliability.", this);
            }
        }

        if (robot == null)
            Debug.LogError("[MG2Manager] No robot found. Assign the robot Transform in the Inspector, or tag the robot GameObject as 'Robot'.", this);

        if (disableRobotWasdMovement && robot != null)
        {
            RobotMovement rm = robot.GetComponentInParent<RobotMovement>();
            if (rm != null)
                rm.SetMovementEnabled(false);
        }

        if (playIntroCameraSequence && audioCardTarget != null)
        {
            yield return RunIntroCameraSequence();
        }

        StartMiniGame();
    }

    private void Update()
    {
        if (!forceCursorVisibleForTileInput)
            return;

        if (phase == MiniGame2Phase.Completed)
            return;

        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible)
            Cursor.visible = true;
    }

    public void StartMiniGame()
    {
        SetIntroSequenceRunning(false);

        cardReached = false;
        cardInteracted = false;
        hasLoggedInefficiency = false;
        hasLoggedEnergyCritical = false;
        totalActualEnergy = 0f;
        remainingEnergy = startingEnergyBudget;
        idealEnergy = 0f;
        idealPath = null;
        actualPath.Clear();
        lastVisited = null;
        miniGameEnded = false;
        endedByEnergyDepletion = false;
        UpdateAudioCardRangeState(false);
        LastResult = default;

        ResolveRouteCoords();

        if (gridManager != null)
        {
            gridManager.BuildGrid();
            idealPath = FindIdealPathToInteractionRange(startCoord, cardCoord);
            idealEnergy = gridManager.GetPathEnergy(idealPath);
        }

        Log($"[MG2] Start. start={startCoord.x},{startCoord.y} card={cardCoord.x},{cardCoord.y} idealEnergy={idealEnergy:F2} idealSteps={(idealPath != null ? idealPath.Count : 0)} budget={(useEnergyBudget ? startingEnergyBudget.ToString("F2") : "off")}");
        if (debugScoringToConsole)
        {
            if (idealPath == null || idealPath.Count == 0)
                Debug.LogWarning("[MG2][Scoring] Ideal path could not be generated. Evaluation will still run but efficiency scores may be 0.", this);
        }

        UpdateAudioCardRangeState(startCoord);
        SetPhase(MiniGame2Phase.Planning);
    }

    private IEnumerator RunIntroCameraSequence()
    {
        Vector3 introStartWorld = ResolveIntroStartWorld();
        Vector3 introEndWorld = ResolveIntroFocusWorld();

        MG2CinemachineTopDownInput topDownInput = introTopDownInput != null ? introTopDownInput : FindFirstObjectByType<MG2CinemachineTopDownInput>();
        if (topDownInput != null)
        {
            topDownInput.ResolveRuntimeReferencesForExternalUse();
            Transform cameraTarget = topDownInput.CameraTarget;

            if (cameraTarget != null)
            {
                SetIntroSequenceRunning(true);

                bool didLockInput = false;
                if (lockInputDuringIntro && controlManager != null)
                {
                    controlManager.SetInputLocked(true);
                    didLockInput = true;
                }

                bool topDownWasEnabled = topDownInput.enabled;
                if (topDownWasEnabled)
                    topDownInput.enabled = false;

                float targetY = cameraTarget.position.y;
                Vector3 startTarget = introStartWorld;
                startTarget.y = targetY;

                Vector3 revealTarget = introEndWorld;
                revealTarget.y = targetY;

                if (!IsFiniteVector(revealTarget))
                    revealTarget = startTarget;

                if (!IsFiniteVector(startTarget))
                    startTarget = cameraTarget.position;

                if (snapToIntroStartBeforeReveal)
                {
                    cameraTarget.position = startTarget;
                    yield return null;
                }

                if (introDelayBeforeMove > 0f)
                    yield return new WaitForSeconds(introDelayBeforeMove);

                yield return LerpPosition(cameraTarget, cameraTarget.position, revealTarget, introMoveDuration);

                if (introHoldDuration > 0f)
                    yield return new WaitForSeconds(introHoldDuration);

                yield return LerpPosition(cameraTarget, cameraTarget.position, startTarget, introMoveDuration);

                if (introDelayBeforeGameplay > 0f)
                    yield return new WaitForSeconds(introDelayBeforeGameplay);

                if (topDownWasEnabled)
                    topDownInput.enabled = true;

                if (didLockInput && controlManager != null)
                    controlManager.SetInputLocked(false);

                SetIntroSequenceRunning(false);
                yield break;
            }
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (cameras != null && cameras.Length > 0)
                cam = cameras[0];
        }

        if (cam == null)
            yield break;

        SetIntroSequenceRunning(true);

        bool didLockInputFallback = false;
        if (lockInputDuringIntro && controlManager != null)
        {
            controlManager.SetInputLocked(true);
            didLockInputFallback = true;
        }

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        Vector3 introStartPos = introStartCameraPose != null ? introStartCameraPose.position : startPos;
        Quaternion introStartRot = introStartCameraPose != null ? introStartCameraPose.rotation : startRot;

        Vector3 focusPos;
        Quaternion focusRot;

        if (introFocusCameraPose != null)
        {
            focusPos = introFocusCameraPose.position;
            focusRot = introFocusCameraPose.rotation;
        }
        else
        {
            Vector3 offset = startPos - introStartWorld;
            focusPos = introEndWorld + offset;
            focusRot = Quaternion.LookRotation((introEndWorld - focusPos).normalized, Vector3.up);
        }

        if (!IsFiniteVector(introStartPos))
            introStartPos = startPos;
        if (!IsFiniteQuaternion(introStartRot))
            introStartRot = startRot;
        if (!IsFiniteVector(focusPos))
            focusPos = startPos;
        if (!IsFiniteQuaternion(focusRot))
            focusRot = startRot;

        if (snapToIntroStartBeforeReveal)
        {
            cam.transform.position = introStartPos;
            cam.transform.rotation = introStartRot;
            yield return null;
        }

        if (introDelayBeforeMove > 0f)
            yield return new WaitForSeconds(introDelayBeforeMove);

        yield return LerpCamera(cam.transform, cam.transform.position, cam.transform.rotation, focusPos, focusRot, introMoveDuration);

        if (introHoldDuration > 0f)
            yield return new WaitForSeconds(introHoldDuration);

        yield return LerpCamera(cam.transform, cam.transform.position, cam.transform.rotation, introStartPos, introStartRot, introMoveDuration);

        if (introDelayBeforeGameplay > 0f)
            yield return new WaitForSeconds(introDelayBeforeGameplay);

        if (didLockInputFallback && controlManager != null)
            controlManager.SetInputLocked(false);

        SetIntroSequenceRunning(false);
    }

    private Vector3 ResolveIntroStartWorld()
    {
        if (introStartMarker != null)
            return introStartMarker.position;

        if (startPoint != null)
            return startPoint.position;

        if (tileClickMover == null)
            tileClickMover = FindFirstObjectByType<TileClickMover>();

        if (tileClickMover != null && tileClickMover.MoverRoot != null)
            return tileClickMover.MoverRoot.position;

        if (robot != null)
            return robot.position;

        return Vector3.zero;
    }

    private Vector3 ResolveIntroFocusWorld()
    {
        if (introFocusMarker != null)
            return introFocusMarker.position;

        if (audioCardTarget != null)
            return audioCardTarget.position;

        return ResolveIntroStartWorld();
    }

    private void SetIntroSequenceRunning(bool running)
    {
        if (introSequenceRunning == running)
            return;

        introSequenceRunning = running;
        IntroSequenceStateChanged?.Invoke(running);
    }

    private static IEnumerator LerpCamera(Transform cameraTransform, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float duration)
    {
        if (cameraTransform == null)
            yield break;

        float d = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / d;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            cameraTransform.position = Vector3.Lerp(fromPos, toPos, eased);
            cameraTransform.rotation = Quaternion.Slerp(fromRot, toRot, eased);
            yield return null;
        }

        cameraTransform.position = toPos;
        cameraTransform.rotation = toRot;
    }

    private static IEnumerator LerpPosition(Transform targetTransform, Vector3 fromPos, Vector3 toPos, float duration)
    {
        if (targetTransform == null)
            yield break;

        float d = Mathf.Max(0.01f, duration);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / d;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            targetTransform.position = Vector3.Lerp(fromPos, toPos, eased);
            yield return null;
        }

        targetTransform.position = toPos;
    }

    private static bool IsFiniteVector(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }

    private static bool IsFiniteQuaternion(Quaternion q)
    {
        return !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w) || float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w));
    }

    private void ResolveRouteCoords()
    {
        if (gridManager == null)
        {
            startCoord = Vector2Int.zero;
            cardCoord = Vector2Int.zero;
            return;
        }

        Vector3 startWorld = startPoint != null ? startPoint.position : (robot != null ? robot.position : Vector3.zero);
        startCoord = gridManager.WorldToGrid(startWorld);

        if (audioCardTarget != null)
            cardCoord = gridManager.WorldToGrid(audioCardTarget.position);
    }

    private List<Vector2Int> FindIdealPathToInteractionRange(Vector2Int start, Vector2Int card)
    {
        if (gridManager == null)
            return null;

        List<Vector2Int> bestPath = null;
        float bestEnergy = float.PositiveInfinity;
        int bestSteps = int.MaxValue;

        int range = Mathf.Max(0, cardInteractRangeTiles);
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int chebyshev = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                int manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);
                bool withinRange = allowDiagonalCardInteraction ? chebyshev <= range : manhattan <= range;
                if (!withinRange)
                    continue;

                Vector2Int candidateGoal = new Vector2Int(card.x + dx, card.y + dy);
                GridManager.Node goalNode = gridManager.GetNode(candidateGoal.x, candidateGoal.y);
                if (goalNode == null || goalNode.isBlocked)
                    continue;

                List<Vector2Int> candidatePath = gridManager.FindIdealPath(start, candidateGoal);
                if (candidatePath == null || candidatePath.Count == 0)
                    continue;

                float candidateEnergy = gridManager.GetPathEnergy(candidatePath);
                int candidateSteps = Mathf.Max(0, candidatePath.Count - 1);
                bool better = candidateEnergy < bestEnergy ||
                              (Mathf.Approximately(candidateEnergy, bestEnergy) && candidateSteps < bestSteps);
                if (!better)
                    continue;

                bestEnergy = candidateEnergy;
                bestSteps = candidateSteps;
                bestPath = candidatePath;
            }
        }

        return bestPath;
    }

    private void SetPhase(MiniGame2Phase next)
    {
        if (phase == next) return;
        phase = next;
        PhaseChanged?.Invoke(phase);
    }

    private void Log(string msg)
    {
        if (!enableLogging) return;
        Debug.Log(msg);
        LogMessage?.Invoke(msg);
    }

    public void LogPathEvent(string msg)
    {
        LogMessage?.Invoke(msg);
        if (enableLogging) Debug.Log(msg);
    }

    public void NotifyRobotStepProcessed(Vector2Int coord)
    {
        if (phase == MiniGame2Phase.Planning)
        {
            SetPhase(MiniGame2Phase.RobotMoving);
        }

        // Additional hooks can be added later (e.g., show step counters).
    }

    public void RecordTileVisit(Vector2Int coord, float cost, bool isSaving)
    {
        if (miniGameEnded) return;

        if (lastVisited.HasValue && lastVisited.Value == coord)
            return;

        lastVisited = coord;
        actualPath.Add(coord);

        ApplyEnergyUsage(cost, $"step@{coord.x},{coord.y}");
        if (miniGameEnded) return;

        if (isSaving)
        {
            LogMessage?.Invoke("Energy saving surface detected");
        }

        UpdateAudioCardRangeState(coord);

        if (!cardReached && isAudioCardInRange)
        {
            cardReached = true;
            Log("[MG2] Audio card in interaction range");
            LogCheckpointSummary("Audio card checkpoint");
            LogMessage?.Invoke("Audio card in range - press E to interact");
        }

        // Feedback heuristics.
        if (!hasLoggedInefficiency && idealEnergy > 0f && totalActualEnergy > idealEnergy * (1f + inefficiencyTolerance))
        {
            hasLoggedInefficiency = true;
            LogMessage?.Invoke("Path inefficiency detected");
        }

        if (!hasLoggedEnergyCritical && idealEnergy > 0f && totalActualEnergy > idealEnergy * energyCriticalAtIdealMultiplier)
        {
            hasLoggedEnergyCritical = true;
            LogMessage?.Invoke("Energy level critical");
        }
    }

    public bool TryInteractWithAudioCard(Vector2Int robotCoord)
    {
        if (miniGameEnded)
            return false;

        if (cardInteracted)
            return false;

        UpdateAudioCardRangeState(robotCoord);

        if (!isAudioCardInRange)
        {
            LogMessage?.Invoke("Move closer to the audio card to interact");
            return false;
        }

        cardInteracted = true;
        Log("[MG2] Audio card interacted");
        CompleteMiniGame();
        return true;
    }

    private bool IsWithinCardInteractRange(Vector2Int robotCoord)
    {
        int dx = Mathf.Abs(robotCoord.x - cardCoord.x);
        int dy = Mathf.Abs(robotCoord.y - cardCoord.y);

        if (allowDiagonalCardInteraction)
        {
            return Mathf.Max(dx, dy) <= cardInteractRangeTiles;
        }

        return (dx + dy) <= cardInteractRangeTiles;
    }

    private void CompleteMiniGame()
    {
        if (miniGameEnded) return;
        miniGameEnded = true;
        UpdateAudioCardRangeState(false);

        SetPhase(MiniGame2Phase.Completed);

        MiniGame2EvaluationResult result = Evaluate();
        if (endedByEnergyDepletion)
        {
            result.tier = MiniGameTier.Fail;
            result.energyEfficiencyScore = 0f;
            result.pathEfficiencyScore = 0f;
            result.finalScore = 0f;
            result.isSuccess = false;
            result.failedByEnergyDepletion = true;
        }
        else
        {
            float pass = Mathf.Max(50f, learningProfile != null ? learningProfile.passScore : 50f);
            if (result.finalScore < pass)
                result.tier = MiniGameTier.Fail;

            result.isSuccess = result.tier != MiniGameTier.Fail;
            result.failedByEnergyDepletion = false;
        }

        LastResult = result;

        // Same behavior as MiniGame1: apply stat updates once, immediately after final evaluation.
        ApplyAndLogRobotStatUpdate(result);

        Log($"[MG2] Result final={result.finalScore:F1} tier={result.tier} energy={result.energyEfficiencyScore:F1} path={result.pathEfficiencyScore:F1}");
        LogEvaluationBreakdown(result, "Final Evaluation");

        MiniGameCompleted?.Invoke(result);
    }

    private void UpdateAudioCardRangeState(Vector2Int robotCoord)
    {
        UpdateAudioCardRangeState(IsWithinCardInteractRange(robotCoord));
    }

    private void UpdateAudioCardRangeState(bool inRange)
    {
        if (isAudioCardInRange == inRange)
            return;

        isAudioCardInRange = inRange;
        AudioCardInteractRangeChanged?.Invoke(inRange);
    }

    private void ApplyAndLogRobotStatUpdate(MiniGame2EvaluationResult result)
    {
        if (learningProfile == null || robotStats == null)
        {
            Debug.LogWarning($"[MG2][StatsUpdate] updated=NO reason=missing refs profile={(learningProfile != null)} robotStats={(robotStats != null)}", this);
            return;
        }

        float beforeEnergyEfficiency = robotStats.energyEfficiency;
        float beforePathAccuracy = robotStats.pathAccuracy;
        float beforeDecisionConfidence = robotStats.decisionConfidence;

        MiniGame2RobotStatUpdater.ApplyUpdateOnce(learningProfile, robotStats, result);

        float deltaEnergyEfficiency = robotStats.energyEfficiency - beforeEnergyEfficiency;
        float deltaPathAccuracy = robotStats.pathAccuracy - beforePathAccuracy;
        float deltaDecisionConfidence = robotStats.decisionConfidence - beforeDecisionConfidence;

        bool updated =
            !Mathf.Approximately(deltaEnergyEfficiency, 0f) ||
            !Mathf.Approximately(deltaPathAccuracy, 0f) ||
            !Mathf.Approximately(deltaDecisionConfidence, 0f);

        string reason = updated ? "tier applied" : (result.tier == MiniGameTier.Fail ? "fail tier" : "zero deltas");
        Debug.Log(
            $"[MG2][StatsUpdate] updated={(updated ? "YES" : "NO")} tier={result.tier} reason={reason} " +
            $"dEnergyEff={deltaEnergyEfficiency:+0.000;-0.000;0.000} " +
            $"dPathAcc={deltaPathAccuracy:+0.000;-0.000;0.000} " +
            $"dDecisionConf={deltaDecisionConfidence:+0.000;-0.000;0.000} " +
            $"now(energyEff={robotStats.energyEfficiency:F3}, pathAcc={robotStats.pathAccuracy:F3}, decisionConf={robotStats.decisionConfidence:F3})",
            this
        );
    }

    private void ApplyEnergyUsage(float amount, string reason)
    {
        float cost = Mathf.Max(0f, amount);
        if (cost <= 0f) return;

        totalActualEnergy += cost;

        if (!useEnergyBudget)
            return;

        remainingEnergy = Mathf.Max(0f, remainingEnergy - cost);

        if (debugScoringToConsole)
            Debug.Log($"[MG2][Energy] -{cost:F2} ({reason}) => remaining={remainingEnergy:F2}/{startingEnergyBudget:F2}", this);

        if (remainingEnergy <= 0f && !miniGameEnded)
        {
            endedByEnergyDepletion = true;
            LogMessage?.Invoke("Energy depleted - mission failed");
            Log("[MG2] Energy depleted before reaching audio card");
            CompleteMiniGame();
        }
    }

    private void LogCheckpointSummary(string label)
    {
        if (!debugScoringToConsole) return;

        int idealSteps = idealPath != null ? Mathf.Max(0, idealPath.Count - 1) : 0;
        int actualSteps = Mathf.Max(0, actualPath.Count - 1);
        string budget = useEnergyBudget ? $" remaining={remainingEnergy:F2}/{startingEnergyBudget:F2}" : string.Empty;
        Debug.Log($"[MG2][Scoring] {label}: steps={actualSteps}/{idealSteps} energy={totalActualEnergy:F2}/{idealEnergy:F2}{budget}", this);
    }

    private void LogEvaluationBreakdown(MiniGame2EvaluationResult result, string label)
    {
        if (!debugScoringToConsole) return;

        Debug.Log(
            $"[MG2][Scoring] {label} -> Final={result.finalScore:F1} Tier={result.tier} | " +
            $"Energy={result.energyEfficiencyScore:F1} Path={result.pathEfficiencyScore:F1} | " +
            $"ActualEnergy={result.actualEnergy:F2} IdealEnergy={result.idealEnergy:F2} ActualSteps={result.actualStepCount} IdealSteps={result.idealStepCount}",
            this
        );
    }

    private MiniGame2EvaluationResult Evaluate()
    {
        int idealSteps = idealPath != null ? Mathf.Max(0, idealPath.Count - 1) : 0;
        int actualSteps = Mathf.Max(0, actualPath.Count - 1);

        float energyScore = learningProfile != null
            ? learningProfile.ComputeEnergyEfficiencyScore(idealEnergy, totalActualEnergy)
            : ComputeBalancedRatioScore(idealEnergy, totalActualEnergy);

        float pathScore = learningProfile != null
            ? learningProfile.ComputePathEfficiencyScore(idealSteps, actualSteps)
            : ComputeBalancedRatioScore(idealSteps, actualSteps);

        float final = learningProfile != null
            ? learningProfile.ComputeFinalScore(energyScore, pathScore)
            : Mathf.Clamp((energyScore + pathScore) * 0.5f, 0f, 100f);

        MiniGameTier tier = learningProfile != null ? learningProfile.GetTier(final) : (final >= 85f ? MiniGameTier.Excellent : final >= 70f ? MiniGameTier.Good : final >= 50f ? MiniGameTier.Average : MiniGameTier.Fail);

        return new MiniGame2EvaluationResult
        {
            finalScore = final,
            tier = tier,
            energyEfficiencyScore = energyScore,
            pathEfficiencyScore = pathScore,
            actualEnergy = totalActualEnergy,
            idealEnergy = idealEnergy,
            actualStepCount = actualSteps,
            idealStepCount = idealSteps
        };
    }

    private static float ComputeBalancedRatioScore(float a, float b)
    {
        if (a <= 0f || b <= 0f)
            return 0f;

        float low = Mathf.Min(a, b);
        float high = Mathf.Max(a, b);
        return Mathf.Clamp((low / high) * 100f, 0f, 100f);
    }

    private IEnumerator ReturnToGateAndExitRoutine()
    {
        returnSequenceRunning = true;

        try
        {
            if (controlManager == null)
                controlManager = FindFirstObjectByType<ControlManager>();

            if (controlManager != null)
                controlManager.SetInputLocked(true);

            ResolveRouteCoords();

            Vector2Int returnTargetCoord = ResolveReturnDemoTargetCoord();

            bool hasCompletedReturn = false;
            if (gridManager != null && tileClickMover != null)
            {
                Vector2Int currentCoord = tileClickMover.CurrentGridPos;

                if (currentCoord == returnTargetCoord)
                {
                    hasCompletedReturn = true;
                    if (enableReturnDemoLogs)
                        Debug.Log("[MG2] Return demo skipped: robot already at configured return tile.", this);
                }

                List<Vector2Int> fullPath = gridManager.FindIdealPath(currentCoord, returnTargetCoord);
                List<Vector2Int> stepSequence = BuildStepSequenceFromFullPath(fullPath);

                if (!hasCompletedReturn && stepSequence != null && stepSequence.Count > 0)
                {
                    tileClickMover.SetAllowMovementWhenMiniGameCompleted(true);
                    bool queued = tileClickMover.TryQueueStepSequence(stepSequence, true, true);

                    if (queued)
                    {
                        if (enableReturnDemoLogs)
                            Debug.Log($"[MG2] Return demo started. steps={stepSequence.Count} from={currentCoord} to={returnTargetCoord}", this);

                        float estimatedTravelSeconds = EstimateTravelSeconds(currentCoord, stepSequence);
                        float scaledEstimate = Mathf.Max(0f, estimatedTravelSeconds) * Mathf.Max(1f, returnPathTimeoutSafetyMultiplier);
                        float perStepEstimate = stepSequence.Count * returnPathTimeoutSecondsPerStep;
                        float timeoutSeconds = Mathf.Max(returnPathTimeoutSeconds, Mathf.Max(scaledEstimate, perStepEstimate));
                        float timeoutAt = Time.unscaledTime + Mathf.Max(0.25f, timeoutSeconds);
                        while (Time.unscaledTime < timeoutAt)
                        {
                            bool queueEmpty = tileClickMover.QueuedStepCount == 0;
                            bool moverIdle = !tileClickMover.IsMoving;
                            bool reachedTarget = tileClickMover.CurrentGridPos == returnTargetCoord;

                            if (queueEmpty && moverIdle && reachedTarget)
                            {
                                hasCompletedReturn = true;
                                break;
                            }

                            yield return null;
                        }

                        if (!hasCompletedReturn)
                            hasCompletedReturn = tileClickMover.CurrentGridPos == returnTargetCoord;
                    }
                    else if (enableReturnDemoLogs)
                    {
                        Debug.LogWarning("[MG2] Return demo path queue rejected. Falling back to direct transition.", this);
                    }
                }
                else if (!hasCompletedReturn)
                {
                    if (enableReturnDemoLogs)
                        Debug.LogWarning($"[MG2] Return demo path missing from {currentCoord} to {returnTargetCoord}. Transition will continue.", this);
                }
            }
            else if (enableReturnDemoLogs)
            {
                Debug.LogWarning("[MG2] Return demo skipped: missing grid/mover references. Falling back to direct transition.", this);
            }

            if (!hasCompletedReturn && enableReturnDemoLogs)
                Debug.LogWarning($"[MG2] Return demo ended before reaching target. current={tileClickMover.CurrentGridPos} target={returnTargetCoord}. Continuing with scene transition.", this);

            if (holdAtGateBeforeTransitionSeconds > 0f)
                yield return new WaitForSecondsRealtime(holdAtGateBeforeTransitionSeconds);

            GameSessionFlowFlags.RequestMiniGame2ReturnSpawn(
                mainSceneGateAnchorName,
                mainSceneGateFallbackAnchorName,
                mainScenePlayerLocalOffset,
                mainSceneRobotLocalOffset,
                faceSpawnedActorsTowardGate);

            if (string.IsNullOrWhiteSpace(returnToMainSceneName))
            {
                Debug.LogError("[MG2] returnToMainSceneName is empty. Cannot exit MG2.", this);
                yield break;
            }

            SceneTransitionFader.TransitionToScene(returnToMainSceneName, -1, Mathf.Max(0.05f, exitFadeDurationSeconds));
        }
        finally
        {
            if (tileClickMover != null)
                tileClickMover.SetAllowMovementWhenMiniGameCompleted(false);

            returnSequenceRunning = false;
            returnSequenceRoutine = null;
        }
    }

    private static List<Vector2Int> BuildStepSequenceFromFullPath(List<Vector2Int> fullPath)
    {
        if (fullPath == null || fullPath.Count <= 1)
            return null;

        List<Vector2Int> steps = new List<Vector2Int>(fullPath.Count - 1);
        for (int i = 1; i < fullPath.Count; i++)
            steps.Add(fullPath[i]);

        return steps;
    }

    private Vector2Int ResolveReturnDemoTargetCoord()
    {
        if (gridManager == null)
            return startCoord;

        if (returnDemoEndPoint != null)
        {
            FloorTile floorTile = returnDemoEndPoint.GetComponent<FloorTile>();
            if (floorTile == null)
                floorTile = returnDemoEndPoint.GetComponentInParent<FloorTile>();

            if (floorTile != null)
            {
                Vector2Int floorCoord = floorTile.GridCoord;
                if (enableReturnDemoLogs)
                    Debug.Log($"[MG2] Return target resolved from FloorTile '{floorTile.name}' stored={floorCoord} world={gridManager.WorldToGrid(floorTile.transform.position)}", this);
                return floorCoord;
            }

            FloorTile nearestFloorTile = FindNearestFloorTile(returnDemoEndPoint.position, returnDemoNearestTileMaxDistance);
            if (nearestFloorTile != null)
            {
                Vector2Int nearestCoord = nearestFloorTile.GridCoord;
                if (enableReturnDemoLogs)
                    Debug.Log($"[MG2] Return target resolved from nearest FloorTile '{nearestFloorTile.name}' stored={nearestCoord} world={gridManager.WorldToGrid(nearestFloorTile.transform.position)}", this);
                return nearestCoord;
            }

            Vector2Int worldCoord = gridManager.WorldToGrid(returnDemoEndPoint.position);
            if (enableReturnDemoLogs)
                Debug.Log($"[MG2] Return target resolved from Transform '{returnDemoEndPoint.name}' world={returnDemoEndPoint.position} => {worldCoord}", this);
            return worldCoord;
        }

        if (useStartTileAsReturnFallback)
            return startCoord;

        if (tileClickMover != null)
            return tileClickMover.CurrentGridPos;

        return startCoord;
    }

    private float EstimateTravelSeconds(Vector2Int start, List<Vector2Int> stepSequence)
    {
        if (gridManager == null || tileClickMover == null || stepSequence == null || stepSequence.Count == 0)
            return 0f;

        float speed = Mathf.Max(0.01f, tileClickMover.MoveSpeed);
        float sumDistance = 0f;
        Vector2Int from = start;

        for (int i = 0; i < stepSequence.Count; i++)
        {
            Vector2Int to = stepSequence[i];
            Vector3 fromWorld = gridManager.GridToWorld(from.x, from.y);
            Vector3 toWorld = gridManager.GridToWorld(to.x, to.y);
            Vector2 fromFlat = new Vector2(fromWorld.x, fromWorld.z);
            Vector2 toFlat = new Vector2(toWorld.x, toWorld.z);
            sumDistance += Vector2.Distance(fromFlat, toFlat);
            from = to;
        }

        return sumDistance / speed;
    }

    private FloorTile FindNearestFloorTile(Vector3 worldPos, float maxDistance)
    {
        FloorTile[] floorTiles = FindObjectsByType<FloorTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (floorTiles == null || floorTiles.Length == 0)
            return null;

        float bestSqrDistance = float.PositiveInfinity;
        FloorTile best = null;
        float maxSqrDistance = maxDistance > 0f ? (maxDistance * maxDistance) : float.PositiveInfinity;

        for (int i = 0; i < floorTiles.Length; i++)
        {
            FloorTile floorTile = floorTiles[i];
            if (floorTile == null)
                continue;

            float sqrDistance = (floorTile.transform.position - worldPos).sqrMagnitude;
            if (sqrDistance > maxSqrDistance)
                continue;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = floorTile;
            }
        }

        return best;
    }
}
