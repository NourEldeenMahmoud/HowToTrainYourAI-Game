using UnityEngine;

public class SpeedConsistencyChallenge : MiniGame1ChallengeBase
{
    public override MiniGame1ChallengeType ChallengeType => MiniGame1ChallengeType.SpeedConsistency;

    [Header("References")]
    [SerializeField] private Transform robotTransform;
    [SerializeField] private MiniGame1FaultState faultState;

    [Header("Target")]
    [Tooltip("Expected robot travel speed used for speed consistency scoring. Suggested: match normal robot movement speed, often 1.0.")]
    [Min(0.1f)] public float targetSpeed = 1.0f;
    [Tooltip("How long speed samples are collected. Suggested: 5-7 seconds.")]
    [Min(0.25f)] public float sampleWindowSeconds = 5.0f;
    [Tooltip("Seconds ignored at the start so acceleration does not unfairly lower the score. Suggested: 1.0.")]
    [Min(0f)] public float sampleWarmupSeconds = 1.0f;
    [Tooltip("Speed is averaged over this interval before scoring. Higher values reduce frame jitter. Suggested: 0.5.")]
    [Min(0.05f)] public float sampleIntervalSeconds = 0.5f;
    [Tooltip("Adds wobble to robot speed during this challenge (mini-game only). Suggested: 0.10-0.20.")]
    [Range(0f, 0.5f)] public float injectedSpeedWobble = 0.15f;

    private Vector3 lastPos;
    private float startTime;
    private float intervalDistance;
    private float intervalElapsed;
    private bool running;
    private bool loggedEnd;

    // Welford online variance
    private int n;
    private float mean;
    private float m2;

    public override void BeginChallenge()
    {
        if (faultState == null && robotTransform != null)
        {
            faultState = robotTransform.GetComponent<MiniGame1FaultState>();
        }

        running = true;
        startTime = Time.time;
        lastPos = robotTransform != null ? robotTransform.position : Vector3.zero;
        intervalDistance = 0f;
        intervalElapsed = 0f;

        n = 0;
        mean = 0f;
        m2 = 0f;
        loggedEnd = false;
        Debug.Log($"[MG1][Speed] Begin targetSpeed={targetSpeed:F2} window={sampleWindowSeconds:F1}s");

        if (faultState != null)
        {
            faultState.faultsEnabled = true;
            faultState.speedWobbleAmplitude = injectedSpeedWobble;
        }
    }

    private void Update()
    {
        if (!running || robotTransform == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 pos = robotTransform.position;
        Vector3 positionDelta = pos - lastPos;
        positionDelta.y = 0f;
        float distance = positionDelta.magnitude;
        lastPos = pos;

        if (Time.time - startTime < sampleWarmupSeconds)
            return;

        intervalDistance += distance;
        intervalElapsed += dt;

        if (intervalElapsed >= sampleIntervalSeconds)
        {
            AddSpeedSample(intervalDistance / Mathf.Max(0.0001f, intervalElapsed));
            intervalDistance = 0f;
            intervalElapsed = 0f;
        }

        if (Time.time - startTime >= sampleWindowSeconds)
        {
            running = false;
            if (intervalElapsed >= 0.05f)
            {
                AddSpeedSample(intervalDistance / Mathf.Max(0.0001f, intervalElapsed));
                intervalDistance = 0f;
                intervalElapsed = 0f;
            }

            if (!loggedEnd)
            {
                loggedEnd = true;
                Debug.Log($"[MG1][Speed] End meanSpeed={mean:F2} stdDev={GetStdDev():F2} samples={n} warmup={sampleWarmupSeconds:F1}s interval={sampleIntervalSeconds:F2}s");
            }

            if (faultState != null)
            {
                faultState.speedWobbleAmplitude = 0f;
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
        raw.speedTarget = targetSpeed;
        raw.speedStdDev = GetStdDev();
    }

    public override float GetScore0To100(MiniGame1LearningProfileSO profile)
    {
        return MiniGame1ScoringEngine.ScoreSpeed(profile, GetRunMetrics());
    }

    public MiniGame1ChallengeRunMetrics GetRunMetrics()
    {
        return new MiniGame1ChallengeRunMetrics
        {
            hasValue = true,
            averageSpeed = mean,
            speedStdDev = GetStdDev(),
            speedTarget = targetSpeed
        };
    }

    private float GetStdDev()
    {
        if (n <= 1) return 0f;
        float variance = m2 / (n - 1);
        return Mathf.Sqrt(Mathf.Max(0f, variance));
    }

    private void AddSpeedSample(float speed)
    {
        n++;
        float speedDelta = speed - mean;
        mean += speedDelta / n;
        float delta2 = speed - mean;
        m2 += speedDelta * delta2;
    }
}
