using System;
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
    [SerializeField] private Transform chargingStationTarget;
    [SerializeField] private Transform robot;

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

    private MiniGame2Phase phase = MiniGame2Phase.Idle;
    private bool cardReached;
    private bool chargerReached;
    private bool hasLoggedInefficiency;
    private bool hasLoggedEnergyCritical;

    private Vector2Int startCoord;
    private Vector2Int cardCoord;
    private Vector2Int chargerCoord;

    private float totalActualEnergy;
    private float remainingEnergy;
    private float idealEnergy;
    private List<Vector2Int> idealPath;
    private readonly List<Vector2Int> actualPath = new List<Vector2Int>(256);
    private Vector2Int? lastVisited;
    private int collisionCount;
    private bool miniGameEnded;
    private bool endedByEnergyDepletion;

    public event Action<MiniGame2EvaluationResult> MiniGameCompleted;
    public event Action<MiniGame2Phase> PhaseChanged;
    public event Action<string> LogMessage;

    public MiniGame2Phase CurrentPhase => phase;
    public RobotStatsSO RobotStats => robotStats;
    public IReadOnlyList<Vector2Int> IdealPath => idealPath;
    public IReadOnlyList<Vector2Int> ActualPath => actualPath;
    public float IdealEnergy => idealEnergy;
    public float ActualEnergy => totalActualEnergy;
    public float RemainingEnergy => remainingEnergy;
    public MiniGame2EvaluationResult LastResult { get; private set; }
    public bool HasPassedLastRun
    {
        get
        {
            float pass = Mathf.Max(50f, learningProfile != null ? learningProfile.passScore : 50f);
            return LastResult.finalScore >= pass && LastResult.tier != MiniGameTier.Fail;
        }
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

    private void Start()
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
        cardReached = false;
        chargerReached = false;
        hasLoggedInefficiency = false;
        hasLoggedEnergyCritical = false;
        collisionCount = 0;
        totalActualEnergy = 0f;
        remainingEnergy = startingEnergyBudget;
        idealEnergy = 0f;
        idealPath = null;
        actualPath.Clear();
        lastVisited = null;
        miniGameEnded = false;
        endedByEnergyDepletion = false;
        LastResult = default;

        ResolveRouteCoords();

        if (gridManager != null)
        {
            gridManager.BuildGrid();
            (idealPath, idealEnergy) = gridManager.FindIdealFullPath(startCoord, cardCoord, chargerCoord);
        }

        Log($"[MG2] Start. start={startCoord.x},{startCoord.y} card={cardCoord.x},{cardCoord.y} charger={chargerCoord.x},{chargerCoord.y} idealEnergy={idealEnergy:F2} idealSteps={(idealPath != null ? idealPath.Count : 0)} budget={(useEnergyBudget ? startingEnergyBudget.ToString("F2") : "off")}");
        if (debugScoringToConsole)
        {
            if (idealPath == null || idealPath.Count == 0)
                Debug.LogWarning("[MG2][Scoring] Ideal path could not be generated. Evaluation will still run but efficiency scores may be 0.", this);

            if (cardCoord == chargerCoord)
                Debug.LogWarning("[MG2][Scoring] Audio card and charger coords are identical. Completion may happen immediately after card reach.", this);
        }
        SetPhase(MiniGame2Phase.Planning);
    }

    private void ResolveRouteCoords()
    {
        if (gridManager == null)
        {
            startCoord = Vector2Int.zero;
            cardCoord = Vector2Int.zero;
            chargerCoord = Vector2Int.zero;
            return;
        }

        Vector3 startWorld = startPoint != null ? startPoint.position : (robot != null ? robot.position : Vector3.zero);
        startCoord = gridManager.WorldToGrid(startWorld);

        if (audioCardTarget != null)
            cardCoord = gridManager.WorldToGrid(audioCardTarget.position);
        if (chargingStationTarget != null)
            chargerCoord = gridManager.WorldToGrid(chargingStationTarget.position);
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
        if (miniGameEnded || chargerReached) return;

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

        if (!cardReached && coord == cardCoord)
        {
            cardReached = true;
            Log("[MG2] Audio card reached");
            LogCheckpointSummary("Audio card checkpoint");
            if (debugScoringToConsole)
                Debug.Log("[MG2][Scoring] Evaluation is final only after reaching the charging station.", this);
        }

        if (cardReached && !chargerReached && coord == chargerCoord)
        {
            chargerReached = true;
            Log("[MG2] Charging station reached");
            CompleteMiniGame();
            return;
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

    public void RecordCollision()
    {
        // Collision tracking is disabled for scoring in MG2.
    }

    private void CompleteMiniGame()
    {
        if (miniGameEnded) return;
        miniGameEnded = true;

        SetPhase(MiniGame2Phase.Completed);

        MiniGame2EvaluationResult result = Evaluate();
        if (endedByEnergyDepletion)
        {
            float failCap = learningProfile != null ? learningProfile.passScore - 0.01f : 49.99f;
            result.tier = MiniGameTier.Fail;
            result.finalScore = Mathf.Clamp(Mathf.Min(result.finalScore, failCap), 0f, 100f);
        }
        else
        {
            float pass = Mathf.Max(50f, learningProfile != null ? learningProfile.passScore : 50f);
            if (result.finalScore < pass)
                result.tier = MiniGameTier.Fail;
        }

        LastResult = result;

        // Same behavior as MiniGame1: apply stat updates once, immediately after final evaluation.
        ApplyAndLogRobotStatUpdate(result);

        Log($"[MG2] Result final={result.finalScore:F1} tier={result.tier} energy={result.energyEfficiencyScore:F1} path={result.pathEfficiencyScore:F1}");
        LogEvaluationBreakdown(result, "Final Evaluation");

        MiniGameCompleted?.Invoke(result);
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
            Log("[MG2] Energy depleted before reaching charger");
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
            : (idealEnergy > 0f && totalActualEnergy > 0f ? Mathf.Clamp((idealEnergy / totalActualEnergy) * 100f, 0f, 100f) : 0f);

        float pathScore = learningProfile != null
            ? learningProfile.ComputePathEfficiencyScore(idealSteps, actualSteps)
            : (idealSteps > 0 && actualSteps > 0 ? Mathf.Clamp(((float)idealSteps / actualSteps) * 100f, 0f, 100f) : 0f);

        float final = learningProfile != null
            ? learningProfile.ComputeFinalScoreWithoutCollision(energyScore, pathScore)
            : Mathf.Clamp((energyScore + pathScore) * 0.5f, 0f, 100f);

        MiniGameTier tier = learningProfile != null ? learningProfile.GetTier(final) : (final >= 85f ? MiniGameTier.Excellent : final >= 70f ? MiniGameTier.Good : final >= 50f ? MiniGameTier.Average : MiniGameTier.Fail);

        return new MiniGame2EvaluationResult
        {
            finalScore = final,
            tier = tier,
            energyEfficiencyScore = energyScore,
            pathEfficiencyScore = pathScore,
            collisionSafetyScore = 0f,
            actualEnergy = totalActualEnergy,
            idealEnergy = idealEnergy,
            collisionCount = 0,
            actualStepCount = actualSteps,
            idealStepCount = idealSteps
        };
    }
}
