using UnityEngine;

public class DriftChallenge : MiniGame1ChallengeBase
{
    public override MiniGame1ChallengeType ChallengeType => MiniGame1ChallengeType.Drift;

    [Header("References")]
    [SerializeField] private Transform robotTransform;
    [SerializeField] private RobotMovement robotMovement;
    [SerializeField] private MiniGame1FaultState faultState;

    [Header("Drift Setup")]
    [Tooltip("Simulated drift offset (+left/-right) from the desired track direction. Suggested: +/-30 to +/-45 degrees.")]
    [Range(-90f, 90f)] public float driftAngleDeg = 45f;
    [Tooltip("How long the player has to stabilize for scoring. Suggested: 4-6 seconds.")]
    [Min(0.25f)] public float challengeDurationSeconds = 4.0f;

    [Header("Scoring")]
    [Tooltip("If heading error drops below this, we consider player started correcting. Suggested: 15-25 degrees.")]
    [Min(0.1f)] public float correctionStartThresholdDeg = 20f;
    [Tooltip("Heading error below this is considered stable. Currently reserved for tuning/feedback. Suggested: 6-10 degrees.")]
    [Min(0.1f)] public float stableThresholdDeg = 8f;

    private float startTime;
    private float correctionStartTime = -1f;
    private float integratedAbsError;
    private int samples;
    private bool running;
    private bool loggedEnd;
    private Vector3 lastRobotPosition;

    public override void BeginChallenge()
    {
        ResolveReferences();

        running = true;
        startTime = Time.time;
        correctionStartTime = -1f;
        integratedAbsError = 0f;
        samples = 0;
        loggedEnd = false;
        lastRobotPosition = robotTransform != null ? robotTransform.position : Vector3.zero;
        Debug.Log($"[MG1][Drift] Begin driftAngle={driftAngleDeg} duration={challengeDurationSeconds:F1}s");

        if (faultState != null)
        {
            faultState.faultsEnabled = true;
            faultState.yawDriftDeg = driftAngleDeg;
            if (robotMovement != null)
                robotMovement.SetMiniGameFaultState(faultState);
            Debug.Log($"[MG1][Drift] Applying fault on '{faultState.gameObject.name}' yaw={faultState.yawDriftDeg:F1}");
        }
    }

    private void Update()
    {
        if (!running) return;

        if (robotTransform == null || robotMovement == null || faultState == null)
            ResolveReferences();

        if (robotTransform == null) return;

        Vector3 movementDelta = robotTransform.position - lastRobotPosition;
        lastRobotPosition = robotTransform.position;

        if (robotMovement == null)
            robotMovement = ResolveRobotMovement();

        Vector2 moveInput = robotMovement != null ? robotMovement.MoveInput : Vector2.zero;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            float inputAngleDeg = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            float stabilizedError = Mathf.Abs(Mathf.DeltaAngle(0f, inputAngleDeg + driftAngleDeg));
            AddSample(stabilizedError);
        }
        else if (movementDelta.sqrMagnitude > 0.0001f)
        {
            Vector3 actualMoveDir = Flatten(movementDelta);
            Vector3 expectedNoDriftDir = ResolveExpectedNoDriftDirection();
            float stabilizedError = Vector3.Angle(actualMoveDir, expectedNoDriftDir);
            AddSample(stabilizedError);
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
        return MiniGame1ScoringEngine.ScoreDrift(profile, GetRunMetrics());
    }

    public MiniGame1ChallengeRunMetrics GetRunMetrics()
    {
        return new MiniGame1ChallengeRunMetrics
        {
            hasValue = true,
            responseTimeSeconds = correctionStartTime > 0f ? (correctionStartTime - startTime) : challengeDurationSeconds,
            averageErrorDeg = samples <= 0 ? 180f : (integratedAbsError / samples)
        };
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }

    private void AddSample(float stabilizedError)
    {
        integratedAbsError += stabilizedError;
        samples++;

        if (correctionStartTime < 0f && stabilizedError < correctionStartThresholdDeg)
            correctionStartTime = Time.time;
    }

    private Vector3 ResolveExpectedNoDriftDirection()
    {
        if (robotMovement != null && robotMovement.orientation != null)
            return Flatten(robotMovement.orientation.forward);

        return robotTransform != null ? Flatten(robotTransform.forward) : Vector3.forward;
    }

    private RobotMovement ResolveRobotMovement()
    {
        RobotMovement movement = robotTransform != null ? robotTransform.GetComponent<RobotMovement>() : null;
        if (movement == null && robotTransform != null)
            movement = robotTransform.GetComponentInParent<RobotMovement>();
        if (movement == null && robotTransform != null)
            movement = robotTransform.GetComponentInChildren<RobotMovement>();
        if (movement == null)
            movement = FindFirstObjectByType<RobotMovement>();
        return movement;
    }

    private void ResolveReferences()
    {
        if (robotMovement == null)
            robotMovement = ResolveRobotMovement();

        if (robotTransform == null && robotMovement != null)
            robotTransform = robotMovement.transform;

        if (robotTransform == null && faultState != null)
            robotTransform = faultState.transform;

        if (faultState == null && robotTransform != null)
            faultState = robotTransform.GetComponent<MiniGame1FaultState>();

        if (faultState == null && robotMovement != null)
            faultState = robotMovement.GetComponent<MiniGame1FaultState>();

        if (faultState == null && robotTransform != null)
            faultState = robotTransform.GetComponentInParent<MiniGame1FaultState>();

        if (faultState == null && robotTransform != null)
            faultState = robotTransform.GetComponentInChildren<MiniGame1FaultState>(true);

        if (faultState == null && robotMovement != null)
            faultState = robotMovement.gameObject.AddComponent<MiniGame1FaultState>();
    }
}
