using UnityEngine;

[CreateAssetMenu(menuName = "HowToTrainYourAI/Robot/RobotStatsSO", fileName = "RobotStatsSO")]
public class RobotStatsSO : ScriptableObject
{
    [Header("Calibration State")]
    [Tooltip("Becomes true after Mini Game 1 is passed once and its post-calibration rates are saved.")]
    public bool hasSavedCalibrationResult;

    [Header("Core Stats (0..1)")]
    [Range(0f, 1f)] public float stability = 0.40f;
    [Range(0f, 1f)] public float pathAccuracy = 0.40f;
    [Range(0f, 1f)] public float inputResponsiveness = 0.40f;

    [Header("Post-MiniGame2 Stats (0..1)")]
    [Range(0f, 1f)] public float energyEfficiency = 0.40f;
    [Range(0f, 1f)] public float decisionConfidence = 0.40f;

    [Header("Post-MiniGame Error Rates (0..1)")]
    [Tooltip("Probability/intensity driver for yaw drift issues after calibration.")]
    [Range(0f, 1f)] public float driftErrorRate = 0.50f;
    [Tooltip("Probability/intensity driver for camera misalignment/instability after calibration.")]
    [Range(0f, 1f)] public float cameraErrorRate = 0.50f;
    [Tooltip("Probability/intensity driver for speed wobble/instability after calibration.")]
    [Range(0f, 1f)] public float speedErrorRate = 0.50f;

    public void ResetSavedCalibrationResult()
    {
        hasSavedCalibrationResult = false;
        driftErrorRate = 0f;
        cameraErrorRate = 0f;
        speedErrorRate = 0f;
    }

    public void ApplyDelta(float stabilityDelta, float pathAccuracyDelta, float responsivenessDelta)
    {
        stability = Mathf.Clamp01(stability + stabilityDelta);
        pathAccuracy = Mathf.Clamp01(pathAccuracy + pathAccuracyDelta);
        inputResponsiveness = Mathf.Clamp01(inputResponsiveness + responsivenessDelta);
    }

    public void ApplyMiniGame2Delta(float energyEfficiencyDelta, float pathAccuracyDelta, float decisionConfidenceDelta)
    {
        energyEfficiency = Mathf.Clamp01(energyEfficiency + energyEfficiencyDelta);
        pathAccuracy = Mathf.Clamp01(pathAccuracy + pathAccuracyDelta);
        decisionConfidence = Mathf.Clamp01(decisionConfidence + decisionConfidenceDelta);
    }

    public void SetErrorRates(float drift, float camera, float speed)
    {
        driftErrorRate = Mathf.Clamp01(drift);
        cameraErrorRate = Mathf.Clamp01(camera);
        speedErrorRate = Mathf.Clamp01(speed);
        hasSavedCalibrationResult = true;
    }
}

