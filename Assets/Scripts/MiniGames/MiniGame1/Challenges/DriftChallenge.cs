using UnityEngine;

public class DriftChallenge : MiniGame1ChallengeBase
{
    public override MiniGame1ChallengeType ChallengeType => MiniGame1ChallengeType.Drift;

    [Header("References")]
    [SerializeField] private Transform robotTransform;
    [SerializeField] private TrackProgress trackProgress;
    [SerializeField] private MiniGame1FaultState faultState;

    [Header("Drift Setup")]
    [Tooltip("Simulated drift offset (+left/-right) from the desired track direction.")]
    [Range(-90f, 90f)] public float driftAngleDeg = 45f;
    [Tooltip("How long the player has to stabilize for scoring.")]
    [Min(0.25f)] public float challengeDurationSeconds = 4.0f;

    [Header("Scoring")]
    [Tooltip("If heading error drops below this, we consider player started correcting.")]
    [Min(0.1f)] public float correctionStartThresholdDeg = 20f;
    [Tooltip("Heading error below this is considered 'stable'.")]
    [Min(0.1f)] public float stableThresholdDeg = 8f;

    private float startTime;
    private float correctionStartTime = -1f;
    private float integratedAbsError;
    private int samples;
    private bool running;
    private bool loggedEnd;

    public override void BeginChallenge()
    {
        if (faultState == null && robotTransform != null)
        {
            faultState = robotTransform.GetComponent<MiniGame1FaultState>();
        }

        running = true;
        startTime = Time.time;
        correctionStartTime = -1f;
        integratedAbsError = 0f;
        samples = 0;
        loggedEnd = false;
        Debug.Log($"[MG1][Drift] Begin driftAngle={driftAngleDeg} duration={challengeDurationSeconds:F1}s");

        if (faultState != null)
        {
            faultState.faultsEnabled = true;
            faultState.yawDriftDeg = driftAngleDeg;
        }
    }

    private void Update()
    {
        if (!running || robotTransform == null || trackProgress == null) return;

        Vector3 desiredDir = trackProgress.GetCurrentSegmentDirection();
        Vector3 driftedDir = Quaternion.Euler(0f, driftAngleDeg, 0f) * desiredDir;

        float headingError = Vector3.Angle(Flatten(robotTransform.forward), Flatten(driftedDir));
        integratedAbsError += headingError;
        samples++;

        float stabilizedError = Vector3.Angle(Flatten(robotTransform.forward), Flatten(desiredDir));

        if (correctionStartTime < 0f && stabilizedError < correctionStartThresholdDeg)
        {
            correctionStartTime = Time.time;
        }

        if (Time.time - startTime >= challengeDurationSeconds)
        {
            running = false;
            if (!loggedEnd)
            {
                loggedEnd = true;
                float avgErr = samples <= 0 ? 180f : (integratedAbsError / samples);
                float rt = correctionStartTime > 0f ? (correctionStartTime - startTime) : challengeDurationSeconds;
                Debug.Log($"[MG1][Drift] End avgErr={avgErr:F1}deg responseTime={rt:F2}s samples={samples}");
            }

            if (faultState != null)
            {
                faultState.yawDriftDeg = 0f;
            }
        }
    }

    public bool IsComplete()
    {
        return !running;
    }

    public override void EndChallenge()
    {
        running = false;
    }

    public override void ContributeToMetrics(ref MiniGame1RawMetrics raw)
    {
        // Response time: time until player heading gets back near track direction.
        if (correctionStartTime > 0f)
        {
            raw.AddResponseTime(correctionStartTime - startTime);
        }
        else
        {
            // Worst-case: never corrected in time window.
            raw.AddResponseTime(challengeDurationSeconds);
        }

        // Correction accuracy proxy: average heading error versus drifted direction.
        float avgErr = samples <= 0 ? 180f : (integratedAbsError / samples);
        raw.AddCorrectionErrorDeg(avgErr);
    }

    public override float GetScore0To100(MiniGame1LearningProfileSO profile)
    {
        // Score combines: (1) correction response time, (2) average error magnitude.
        float avgErr = samples <= 0 ? 180f : (integratedAbsError / samples);

        float responseTime = correctionStartTime > 0f ? (correctionStartTime - startTime) : challengeDurationSeconds;
        float responseScore = MiniGame1Scoring.ResponseTimeScore(profile, responseTime);

        // Normalize error: 0 deg => 100, 45 deg => 0 (soft clamp beyond).
        float errScore = Mathf.Clamp01(1f - (avgErr / 45f)) * 100f;

        return Mathf.Clamp01((0.55f * (responseScore / 100f)) + (0.45f * (errScore / 100f))) * 100f;
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }
}

