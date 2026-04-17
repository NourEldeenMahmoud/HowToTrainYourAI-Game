using UnityEngine;

public class SpeedConsistencyChallenge : MiniGame1ChallengeBase
{
    public override MiniGame1ChallengeType ChallengeType => MiniGame1ChallengeType.SpeedConsistency;

    [Header("References")]
    [SerializeField] private Transform robotTransform;
    [SerializeField] private MiniGame1FaultState faultState;

    [Header("Target")]
    [Min(0.1f)] public float targetSpeed = 1.0f;
    [Min(0.25f)] public float sampleWindowSeconds = 5.0f;
    [Tooltip("Adds wobble to robot speed during this challenge (mini-game only).")]
    [Range(0f, 0.5f)] public float injectedSpeedWobble = 0.15f;

    private Vector3 lastPos;
    private float startTime;
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
        float speed = Vector3.Distance(pos, lastPos) / dt;
        lastPos = pos;

        n++;
        float delta = speed - mean;
        mean += delta / n;
        float delta2 = speed - mean;
        m2 += delta * delta2;

        if (Time.time - startTime >= sampleWindowSeconds)
        {
            running = false;
            if (!loggedEnd)
            {
                loggedEnd = true;
                Debug.Log($"[MG1][Speed] End meanSpeed={mean:F2} stdDev={GetStdDev():F2} samples={n}");
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
        // Lower stddev => higher score. Map 0 => 100, 0.5*target => 0.
        float std = GetStdDev();
        float worst = Mathf.Max(0.01f, targetSpeed * 0.5f);
        return Mathf.Clamp01(1f - (std / worst)) * 100f;
    }

    private float GetStdDev()
    {
        if (n <= 1) return 0f;
        float variance = m2 / (n - 1);
        return Mathf.Sqrt(Mathf.Max(0f, variance));
    }
}

