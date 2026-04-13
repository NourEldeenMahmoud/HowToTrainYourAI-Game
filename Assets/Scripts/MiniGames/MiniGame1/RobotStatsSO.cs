using UnityEngine;

[CreateAssetMenu(menuName = "HowToTrainYourAI/Robot/RobotStatsSO", fileName = "RobotStatsSO")]
public class RobotStatsSO : ScriptableObject
{
    [Header("Core Stats (0..1)")]
    [Range(0f, 1f)] public float stability = 0.40f;
    [Range(0f, 1f)] public float pathAccuracy = 0.40f;
    [Range(0f, 1f)] public float inputResponsiveness = 0.40f;

    [Header("Post-MiniGame Error Rates (0..1)")]
    [Tooltip("Probability/intensity driver for yaw drift issues after calibration.")]
    [Range(0f, 1f)] public float driftErrorRate = 0.50f;
    [Tooltip("Probability/intensity driver for camera misalignment/instability after calibration.")]
    [Range(0f, 1f)] public float cameraErrorRate = 0.50f;
    [Tooltip("Probability/intensity driver for speed wobble/instability after calibration.")]
    [Range(0f, 1f)] public float speedErrorRate = 0.50f;

    public void ApplyDelta(float stabilityDelta, float pathAccuracyDelta, float responsivenessDelta)
    {
        stability = Mathf.Clamp01(stability + stabilityDelta);
        pathAccuracy = Mathf.Clamp01(pathAccuracy + pathAccuracyDelta);
        inputResponsiveness = Mathf.Clamp01(inputResponsiveness + responsivenessDelta);
    }

    public void SetErrorRates(float drift, float camera, float speed)
    {
        driftErrorRate = Mathf.Clamp01(drift);
        cameraErrorRate = Mathf.Clamp01(camera);
        speedErrorRate = Mathf.Clamp01(speed);
    }
}

