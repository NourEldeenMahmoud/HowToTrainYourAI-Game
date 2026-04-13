using System.Collections;
using UnityEngine;

public class MiniGame1Manager : MonoBehaviour
{
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

    public MiniGame1EvaluationResult LastResult { get; private set; }

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
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        isRunning = true;
        raw.Reset();

        if (enableLogging)
        {
            Debug.Log("[MG1] RunSequence start");
            Debug.Log($"[MG1] Refs profile={(learningProfile != null ? learningProfile.name : "NULL")}, robotStats={(robotStats != null ? robotStats.name : "NULL")}, trackProgress={(trackProgress != null ? trackProgress.name : "NULL")}");
        }

        if (initialFreeMoveSeconds > 0f)
        {
            if (enableLogging) Debug.Log($"[MG1] FreeMove (initial) {initialFreeMoveSeconds:F1}s");
            yield return new WaitForSeconds(initialFreeMoveSeconds);
        }

        if (trackAccuracyTracker != null)
        {
            trackAccuracyTracker.ResetTracking();
        }

        MiniGame1ChallengeScores challengeScores = new MiniGame1ChallengeScores();

        if (driftLeft != null)
        {
            if (enableLogging) Debug.Log("[MG1] DriftLeft begin");
            driftLeft.BeginChallenge();
            yield return WaitUntilComplete(driftLeft);
            driftLeft.ContributeToMetrics(ref raw);
            challengeScores.driftScore = driftLeft.GetScore0To100(learningProfile);
            if (enableLogging) Debug.Log($"[MG1] DriftLeft end score={challengeScores.driftScore:F1}");
        }

        if (freeMoveBetweenChallengesSeconds > 0f)
        {
            if (enableLogging) Debug.Log($"[MG1] FreeMove (between) {freeMoveBetweenChallengesSeconds:F1}s");
            yield return new WaitForSeconds(freeMoveBetweenChallengesSeconds);
        }

        if (driftRight != null)
        {
            if (enableLogging) Debug.Log("[MG1] DriftRight begin");
            driftRight.BeginChallenge();
            yield return WaitUntilComplete(driftRight);
            driftRight.ContributeToMetrics(ref raw);
            float rightScore = driftRight.GetScore0To100(learningProfile);
            challengeScores.driftScore = Mathf.Clamp01((challengeScores.driftScore + rightScore) / 200f) * 100f;
            if (enableLogging) Debug.Log($"[MG1] DriftRight end score={rightScore:F1} (combined driftScore={challengeScores.driftScore:F1})");
        }

        if (freeMoveBetweenChallengesSeconds > 0f)
        {
            if (enableLogging) Debug.Log($"[MG1] FreeMove (between) {freeMoveBetweenChallengesSeconds:F1}s");
            yield return new WaitForSeconds(freeMoveBetweenChallengesSeconds);
        }

        if (cameraAlignment != null)
        {
            if (enableLogging) Debug.Log("[MG1] CameraAlignment begin");
            cameraAlignment.BeginChallenge();
            yield return WaitUntilComplete(cameraAlignment);
            cameraAlignment.ContributeToMetrics(ref raw);
            challengeScores.cameraScore = cameraAlignment.GetScore0To100(learningProfile);
            if (enableLogging) Debug.Log($"[MG1] CameraAlignment end score={challengeScores.cameraScore:F1}");
        }

        if (freeMoveBetweenChallengesSeconds > 0f)
        {
            if (enableLogging) Debug.Log($"[MG1] FreeMove (between) {freeMoveBetweenChallengesSeconds:F1}s");
            yield return new WaitForSeconds(freeMoveBetweenChallengesSeconds);
        }

        if (speedConsistency != null)
        {
            if (enableLogging) Debug.Log("[MG1] SpeedConsistency begin");
            speedConsistency.BeginChallenge();
            yield return WaitUntilComplete(speedConsistency);
            speedConsistency.ContributeToMetrics(ref raw);
            challengeScores.speedScore = speedConsistency.GetScore0To100(learningProfile);
            if (enableLogging) Debug.Log($"[MG1] SpeedConsistency end score={challengeScores.speedScore:F1}");
        }

        if (trackAccuracyTracker != null)
        {
            raw.averageLateralDistanceMeters = trackAccuracyTracker.GetAverageLateralDistance();
        }

        if (enableLogging)
        {
            Debug.Log($"[MG1] Raw avgLateral={raw.averageLateralDistanceMeters:F2}m avgRT={raw.GetAverageResponseTime():F2}s avgCorrErr={raw.GetAverageCorrectionErrorDeg():F1}deg camErr={raw.GetAverageCameraErrorDeg():F1}deg speedStd={raw.speedStdDev:F2} targetSpeed={raw.speedTarget:F2}");
        }

        LastResult = MiniGame1Evaluator.Evaluate(learningProfile, raw, challengeScores);

        if (enableLogging)
        {
            Debug.Log($"[MG1] Result final={LastResult.finalScore:F1} tier={LastResult.tier} drift={LastResult.challengeScores.driftScore:F1} cam={LastResult.challengeScores.cameraScore:F1} speed={LastResult.challengeScores.speedScore:F1}");
        }

        // Update robot stats once at end, only if passed.
        MiniGame1RobotStatUpdater.ApplyUpdateOnce(learningProfile, robotStats, LastResult);

        if (enableLogging && robotStats != null)
        {
            Debug.Log($"[MG1] RobotStats now stability={robotStats.stability:F2} pathAcc={robotStats.pathAccuracy:F2} resp={robotStats.inputResponsiveness:F2} driftRate={robotStats.driftErrorRate:F2} camRate={robotStats.cameraErrorRate:F2} speedRate={robotStats.speedErrorRate:F2}");
        }

        isRunning = false;
        if (enableLogging) Debug.Log("[MG1] RunSequence end");
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

