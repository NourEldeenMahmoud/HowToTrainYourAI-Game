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
    [Tooltip("When actual energy exceeds idealEnergy * (1 + tolerance), emit 'Path inefficiency detected'.")]
    [SerializeField, Range(0f, 2f)] private float inefficiencyTolerance = 0.15f;
    [Tooltip("Emit 'Energy level critical' when actual energy exceeds this fraction of the ideal energy (after ideal is computed).")]
    [SerializeField, Range(0f, 5f)] private float energyCriticalAtIdealMultiplier = 1.25f;

    private MiniGame2Phase phase = MiniGame2Phase.Idle;
    private bool cardReached;
    private bool chargerReached;
    private bool hasLoggedInefficiency;
    private bool hasLoggedEnergyCritical;

    private Vector2Int startCoord;
    private Vector2Int cardCoord;
    private Vector2Int chargerCoord;

    private float totalActualEnergy;
    private float idealEnergy;
    private List<Vector2Int> idealPath;
    private readonly List<Vector2Int> actualPath = new List<Vector2Int>(256);
    private Vector2Int? lastVisited;
    private int collisionCount;

    public event Action<MiniGame2EvaluationResult> MiniGameCompleted;
    public event Action<MiniGame2Phase> PhaseChanged;
    public event Action<string> LogMessage;

    public MiniGame2Phase CurrentPhase => phase;
    public RobotStatsSO RobotStats => robotStats;
    public IReadOnlyList<Vector2Int> IdealPath => idealPath;
    public IReadOnlyList<Vector2Int> ActualPath => actualPath;
    public float IdealEnergy => idealEnergy;
    public float ActualEnergy => totalActualEnergy;

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (controlManager == null) controlManager = FindFirstObjectByType<ControlManager>();
        if (tileClickMover == null) tileClickMover = FindFirstObjectByType<TileClickMover>();
        if (robot == null && tileClickMover != null) robot = tileClickMover.transform;

        StartMiniGame();
    }

    public void StartMiniGame()
    {
        cardReached = false;
        chargerReached = false;
        hasLoggedInefficiency = false;
        hasLoggedEnergyCritical = false;
        collisionCount = 0;
        totalActualEnergy = 0f;
        idealEnergy = 0f;
        idealPath = null;
        actualPath.Clear();
        lastVisited = null;

        ResolveRouteCoords();

        if (gridManager != null)
        {
            gridManager.BuildGrid();
            (idealPath, idealEnergy) = gridManager.FindIdealFullPath(startCoord, cardCoord, chargerCoord);
        }

        Log($"[MG2] Start. start={startCoord.x},{startCoord.y} card={cardCoord.x},{cardCoord.y} charger={chargerCoord.x},{chargerCoord.y} idealEnergy={idealEnergy:F2} idealSteps={(idealPath != null ? idealPath.Count : 0)}");
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
        if (chargerReached) return;

        if (lastVisited.HasValue && lastVisited.Value == coord)
            return;

        lastVisited = coord;
        actualPath.Add(coord);

        // Pay energy cost per entered tile (movement step).
        totalActualEnergy += Mathf.Max(0f, cost);

        if (isSaving)
        {
            LogMessage?.Invoke("Energy saving surface detected");
        }

        if (!cardReached && coord == cardCoord)
        {
            cardReached = true;
            Log("[MG2] Audio card reached");
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
        collisionCount++;
    }

    private void CompleteMiniGame()
    {
        SetPhase(MiniGame2Phase.Completed);

        MiniGame2EvaluationResult result = Evaluate();
        Log($"[MG2] Result final={result.finalScore:F1} tier={result.tier} energy={result.energyEfficiencyScore:F1} path={result.pathEfficiencyScore:F1} collision={result.collisionSafetyScore:F1}");

        MiniGameCompleted?.Invoke(result);
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

        float collisionScore = learningProfile != null
            ? learningProfile.ComputeCollisionSafetyScore(collisionCount)
            : Mathf.Clamp(100f - collisionCount * 10f, 0f, 100f);

        float final = learningProfile != null
            ? learningProfile.ComputeFinalScore(energyScore, pathScore, collisionScore)
            : Mathf.Clamp(energyScore * 0.40f + pathScore * 0.35f + collisionScore * 0.25f, 0f, 100f);

        MiniGameTier tier = learningProfile != null ? learningProfile.GetTier(final) : (final >= 85f ? MiniGameTier.Excellent : final >= 70f ? MiniGameTier.Good : final >= 50f ? MiniGameTier.Average : MiniGameTier.Fail);

        return new MiniGame2EvaluationResult
        {
            finalScore = final,
            tier = tier,
            energyEfficiencyScore = energyScore,
            pathEfficiencyScore = pathScore,
            collisionSafetyScore = collisionScore,
            actualEnergy = totalActualEnergy,
            idealEnergy = idealEnergy,
            collisionCount = collisionCount,
            actualStepCount = actualSteps,
            idealStepCount = idealSteps
        };
    }
}

