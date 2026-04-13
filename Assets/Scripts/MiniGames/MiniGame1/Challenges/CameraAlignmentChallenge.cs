using UnityEngine;
using Unity.Cinemachine;

public class CameraAlignmentChallenge : MiniGame1ChallengeBase
{
    public override MiniGame1ChallengeType ChallengeType => MiniGame1ChallengeType.CameraAlignment;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Optional: if you're using Cinemachine vcam (like Robot FreeLook Camera), assign its OrbitalFollow here for pitch injection.")]
    [SerializeField] private CinemachineOrbitalFollow orbitalFollow;

    [Header("Fault Injection (optional)")]
    [Tooltip("If set, we will inject a pitch offset at Begin by rotating this transform locally. Use a dedicated pivot if possible.")]
    [SerializeField] private Transform cameraPitchPivot;
    [Range(-60f, 60f)] public float injectedPitchOffsetDeg = 25f;

    [Header("Target")]
    [Tooltip("Target pitch angle in degrees (local X).")]
    [Range(-80f, 80f)] public float targetPitchDeg = 0f;
    [Min(0.25f)] public float challengeDurationSeconds = 4.0f;

    [Header("Scoring")]
    [Min(0.1f)] public float alignmentThresholdDeg = 6f;

    private float startTime;
    private float alignedStartTime = -1f;
    private float sumAbsError;
    private int samples;
    private bool running;
    private bool loggedEnd;
    private Quaternion pivotBaseLocalRot;
    private float orbitalBaseVerticalValue;

    public override void BeginChallenge()
    {
        running = true;
        startTime = Time.time;
        alignedStartTime = -1f;
        sumAbsError = 0f;
        samples = 0;
        loggedEnd = false;
        Debug.Log($"[MG1][Camera] Begin targetPitch={targetPitchDeg:F1} duration={challengeDurationSeconds:F1}s");

        if (orbitalFollow != null)
        {
            orbitalBaseVerticalValue = orbitalFollow.VerticalAxis.Value;
            orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                orbitalBaseVerticalValue + injectedPitchOffsetDeg,
                orbitalFollow.VerticalAxis.Range.x,
                orbitalFollow.VerticalAxis.Range.y
            );
        }

        if (cameraPitchPivot != null)
        {
            pivotBaseLocalRot = cameraPitchPivot.localRotation;
            cameraPitchPivot.localRotation = pivotBaseLocalRot * Quaternion.Euler(injectedPitchOffsetDeg, 0f, 0f);
        }
    }

    private void Update()
    {
        if (!running || cameraTransform == null) return;

        float pitch = NormalizePitch(cameraTransform.eulerAngles.x);
        float absErr = Mathf.Abs(Mathf.DeltaAngle(pitch, targetPitchDeg));
        sumAbsError += absErr;
        samples++;

        if (alignedStartTime < 0f && absErr <= alignmentThresholdDeg)
        {
            alignedStartTime = Time.time;
        }

        if (Time.time - startTime >= challengeDurationSeconds)
        {
            running = false;
            if (!loggedEnd)
            {
                loggedEnd = true;
                float avgErr = samples <= 0 ? 180f : (sumAbsError / samples);
                float rt = alignedStartTime > 0f ? (alignedStartTime - startTime) : challengeDurationSeconds;
                Debug.Log($"[MG1][Camera] End avgErr={avgErr:F1}deg responseTime={rt:F2}s samples={samples}");
            }

            if (cameraPitchPivot != null)
            {
                cameraPitchPivot.localRotation = pivotBaseLocalRot;
            }

            if (orbitalFollow != null)
            {
                orbitalFollow.VerticalAxis.Value = orbitalBaseVerticalValue;
            }
        }
    }

    public bool IsComplete()
    {
        return !running;
    }

    public override void ContributeToMetrics(ref MiniGame1RawMetrics raw)
    {
        float responseTime = alignedStartTime > 0f ? (alignedStartTime - startTime) : challengeDurationSeconds;
        raw.AddResponseTime(responseTime);

        float avgErr = samples <= 0 ? 180f : (sumAbsError / samples);
        raw.AddCameraErrorDeg(avgErr);
    }

    public override float GetScore0To100(MiniGame1LearningProfileSO profile)
    {
        float responseTime = alignedStartTime > 0f ? (alignedStartTime - startTime) : challengeDurationSeconds;
        float responseScore = MiniGame1Scoring.ResponseTimeScore(profile, responseTime);

        float avgErr = samples <= 0 ? 180f : (sumAbsError / samples);
        float alignmentScore = Mathf.Clamp01(1f - (avgErr / 30f)) * 100f;

        return Mathf.Clamp01((0.5f * (responseScore / 100f)) + (0.5f * (alignmentScore / 100f))) * 100f;
    }

    private static float NormalizePitch(float eulerX)
    {
        float pitch = eulerX;
        if (pitch > 180f) pitch -= 360f;
        return pitch;
    }
}

