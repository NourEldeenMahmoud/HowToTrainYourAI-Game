using System.Collections;
using System;
using UnityEngine;

public class MiniGame1Manager : MonoBehaviour
{
    public enum MiniGame1Phase
    {
        None,
        FreeMoveInitial,
        DriftLeft,
        FreeMoveBetween_DriftLeft_DriftRight,
        DriftRight,
        FreeMoveBetween_DriftRight_Camera,
        CameraAlignment,
        FreeMoveBetween_Camera_Speed,
        SpeedConsistency,
        Completed
    }

    [Header("Data Assets")]
    [SerializeField] private MiniGame1LearningProfileSO learningProfile;
    [SerializeField] private RobotStatsSO robotStats;

    [Header("Track")]
    [SerializeField] private TrackProgress trackProgress;
    [SerializeField] private TrackAccuracyTracker trackAccuracyTracker;

    [Header("Challenges (optional order is: drift -> drift -> camera -> speed)")]
    [SerializeField] private DriftChallenge driftLeft;
    [SerializeField] private DriftChallenge driftRight;
    [SerializeField] private CameraAlignmentChallenge cameraAlignment;
    [SerializeField] private SpeedConsistencyChallenge speedConsistency;

    [Header("Challenges Auto-Resolve (optional)")]
    [Tooltip("If set, the manager will auto-pick challenges from this object (and children) when references are missing/duplicated.")]
    [SerializeField] private Transform challengesRoot;

    [Header("Flow")]
    [Tooltip("If true, auto-starts the mini game on Start().")]
    [SerializeField] private bool autoStart = false;
    [Tooltip("Print MiniGame1 flow, scores, and updates to Console.")]
    [SerializeField] private bool enableLogging = true;

    [Header("Pacing")]
    [Tooltip("Initial time before the first challenge starts (player moves normally).")]
    [Min(0f)] [SerializeField] private float initialFreeMoveSeconds = 10f;
    [Tooltip("Free-move time inserted between challenges (player moves normally).")]
    [Min(0f)] [SerializeField] private float freeMoveBetweenChallengesSeconds = 10f;

    private bool isRunning;
    private MiniGame1RawMetrics raw;
    private MiniGame1Phase phase = MiniGame1Phase.None;

    public MiniGame1EvaluationResult LastResult { get; private set; }

    public event Action<MiniGame1EvaluationResult> MiniGameCompleted;
    public event Action<MiniGame1Phase> PhaseChanged;
    /// <summary>Fired for every log line when enableLogging is true. Subscribe in UI scripts to show in-game logs.</summary>
    public event Action<string> LogMessage;

    public MiniGame1Phase CurrentPhase => phase;
    public RobotStatsSO RobotStats => robotStats;
    public bool HasPassedLastRun => LastResult.finalScore >= 50f;

    private void Start()
    {
        if (autoStart)
        {
            StartMiniGame();
        }
    }

    public void StartMiniGame()
    {
        if (isRunning) return;
        if (robotStats != null)
        {
            // Starting from scratch => clear any previously saved calibration result.
            robotStats.ResetSavedCalibrationResult();
        }
        StartCoroutine(RunSequence());
    }

    private void Log(string msg)
    {
        if (!enableLogging) return;
        Debug.Log(msg);
        LogMessage?.Invoke(msg);
    }

    private void SetPhase(MiniGame1Phase next)
    {
        if (phase == next) return;
        phase = next;
        PhaseChanged?.Invoke(phase);
    }

    private IEnumerator RunSequence()
    {
        isRunning = true;
        raw.Reset();
        SetPhase(MiniGame1Phase.None);

        ResolveChallenges();

        Log("[MG1] RunSequence start");
        Log($"[MG1] Refs profile={(learningProfile != null ? learningProfile.name : "NULL")}, robotStats={(robotStats != null ? robotStats.name : "NULL")}, trackProgress={(trackProgress != null ? trackProgress.name : "NULL")}");
        string left = driftLeft != null ? driftLeft.driftAngleDeg.ToString("F1") : "NULL";
        string right = driftRight != null ? driftRight.driftAngleDeg.ToString("F1") : "NULL";
        Log($"[MG1] Challenges driftLeft={left}, driftRight={right}, cam={(cameraAlignment != null ? "OK" : "NULL")}, speed={(speedConsistency != null ? "OK" : "NULL")}");

        if (initialFreeMoveSeconds > 0f)
        {
            Log($"[MG1] FreeMove (initial) {initialFreeMoveSeconds:F1}s");
            SetPhase(MiniGame1Phase.FreeMoveInitial);
            yield return new WaitForSeconds(initialFreeMoveSeconds);
        }

        if (trackAccuracyTracker != null)
        {
            trackAccuracyTracker.ResetTracking();
        }

        MiniGame1ChallengeScores challengeScores = new MiniGame1ChallengeScores();

        if (driftLeft != null)
        {
            Log("[MG1] DriftLeft begin");
            SetPhase(MiniGame1Phase.DriftLeft);
            driftLeft.BeginChallenge();
            yield return WaitUntilComplete(driftLeft);
            driftLeft.ContributeToMetrics(ref raw);
            challengeScores.driftScore = driftLeft.GetScore0To100(learningProfile);
            Log($"[MG1] DriftLeft end score={challengeScores.driftScore:F1}");
        }

        if (freeMoveBetweenChallengesSeconds > 0f)
        {
            Log($"[MG1] FreeMove (between) {freeMoveBetweenChallengesSeconds:F1}s");
            SetPhase(MiniGame1Phase.FreeMoveBetween_DriftLeft_DriftRight);
            yield return new WaitForSeconds(freeMoveBetweenChallengesSeconds);
        }

        if (driftRight != null)
        {
            Log("[MG1] DriftRight begin");
            SetPhase(MiniGame1Phase.DriftRight);
            driftRight.BeginChallenge();
            yield return WaitUntilComplete(driftRight);
            driftRight.ContributeToMetrics(ref raw);
            float rightScore = driftRight.GetScore0To100(learningProfile);
            challengeScores.driftScore = Mathf.Clamp01((challengeScores.driftScore + rightScore) / 200f) * 100f;
            Log($"[MG1] DriftRight end score={rightScore:F1} (combined driftScore={challengeScores.driftScore:F1})");
        }

        if (freeMoveBetweenChallengesSeconds > 0f)
        {
            Log($"[MG1] FreeMove (between) {freeMoveBetweenChallengesSeconds:F1}s");
            SetPhase(MiniGame1Phase.FreeMoveBetween_DriftRight_Camera);
            yield return new WaitForSeconds(freeMoveBetweenChallengesSeconds);
        }

        if (cameraAlignment != null)
        {
            Log("[MG1] CameraAlignment begin");
            SetPhase(MiniGame1Phase.CameraAlignment);
            cameraAlignment.BeginChallenge();
            yield return WaitUntilComplete(cameraAlignment);
            cameraAlignment.ContributeToMetrics(ref raw);
            challengeScores.cameraScore = cameraAlignment.GetScore0To100(learningProfile);
            Log($"[MG1] CameraAlignment end score={challengeScores.cameraScore:F1}");
        }

        if (freeMoveBetweenChallengesSeconds > 0f)
        {
            Log($"[MG1] FreeMove (between) {freeMoveBetweenChallengesSeconds:F1}s");
            SetPhase(MiniGame1Phase.FreeMoveBetween_Camera_Speed);
            yield return new WaitForSeconds(freeMoveBetweenChallengesSeconds);
        }

        if (speedConsistency != null)
        {
            Log("[MG1] SpeedConsistency begin");
            SetPhase(MiniGame1Phase.SpeedConsistency);
            speedConsistency.BeginChallenge();
            yield return WaitUntilComplete(speedConsistency);
            speedConsistency.ContributeToMetrics(ref raw);
            challengeScores.speedScore = speedConsistency.GetScore0To100(learningProfile);
            Log($"[MG1] SpeedConsistency end score={challengeScores.speedScore:F1}");
        }

        if (trackAccuracyTracker != null)
        {
            raw.averageLateralDistanceMeters = trackAccuracyTracker.GetAverageLateralDistance();
        }

        Log($"[MG1] Raw avgLateral={raw.averageLateralDistanceMeters:F2}m avgRT={raw.GetAverageResponseTime():F2}s avgCorrErr={raw.GetAverageCorrectionErrorDeg():F1}deg camErr={raw.GetAverageCameraErrorDeg():F1}deg speedStd={raw.speedStdDev:F2} targetSpeed={raw.speedTarget:F2}");

        LastResult = MiniGame1Evaluator.Evaluate(learningProfile, raw, challengeScores);

        Log($"[MG1] Result final={LastResult.finalScore:F1} tier={LastResult.tier} drift={LastResult.challengeScores.driftScore:F1} cam={LastResult.challengeScores.cameraScore:F1} speed={LastResult.challengeScores.speedScore:F1}");

        // Update robot stats once at end, only if passed.
        MiniGame1RobotStatUpdater.ApplyUpdateOnce(learningProfile, robotStats, LastResult);

        if (robotStats != null)
            Log($"[MG1] RobotStats now stability={robotStats.stability:F2} pathAcc={robotStats.pathAccuracy:F2} resp={robotStats.inputResponsiveness:F2} driftRate={robotStats.driftErrorRate:F2} camRate={robotStats.cameraErrorRate:F2} speedRate={robotStats.speedErrorRate:F2}");

        isRunning = false;
        Log("[MG1] RunSequence end");

        SetPhase(MiniGame1Phase.Completed);

        Log("[MG1] Invoking MiniGameCompleted event");
        MiniGameCompleted?.Invoke(LastResult);
    }

    private void ResolveChallenges()
    {
        // 1) Prefer an explicit root if provided.
        Transform root = challengesRoot;
        if (root == null)
        {
            // 2) Fallback: infer from any assigned challenge.
            if (driftLeft != null) root = driftLeft.transform;
            else if (driftRight != null) root = driftRight.transform;
            else if (cameraAlignment != null) root = cameraAlignment.transform;
            else if (speedConsistency != null) root = speedConsistency.transform;
        }

        if (root == null) return;

        // If drift references are missing or duplicated, pick the first two DriftChallenge components we can find.
        if (driftLeft == null || driftRight == null || driftLeft == driftRight)
        {
            DriftChallenge[] drifts = root.GetComponentsInChildren<DriftChallenge>(true);
            if (drifts != null && drifts.Length > 0)
            {
                driftLeft = drifts[0];
                driftRight = drifts.Length > 1 ? drifts[1] : null;
            }
        }

        // If other references are missing, try to resolve them too (first match wins).
        if (cameraAlignment == null)
        {
            cameraAlignment = root.GetComponentInChildren<CameraAlignmentChallenge>(true);
        }

        if (speedConsistency == null)
        {
            speedConsistency = root.GetComponentInChildren<SpeedConsistencyChallenge>(true);
        }
    }

    private static IEnumerator WaitUntilComplete(DriftChallenge drift)
    {
        while (drift != null && !drift.IsComplete())
            yield return null;
    }

    private static IEnumerator WaitUntilComplete(CameraAlignmentChallenge cam)
    {
        while (cam != null && !cam.IsComplete())
            yield return null;
    }

    private static IEnumerator WaitUntilComplete(SpeedConsistencyChallenge speed)
    {
        while (speed != null && !speed.IsComplete())
            yield return null;
    }
}

