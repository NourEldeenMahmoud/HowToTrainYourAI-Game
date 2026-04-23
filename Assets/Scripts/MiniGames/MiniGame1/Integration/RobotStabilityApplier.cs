using UnityEngine;

public class RobotStabilityApplier : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RobotStatsSO robotStats;
    private MiniGame1Manager miniGame1Manager;

    [Header("Drift (movement)")]
    [Tooltip("Max yaw drift in degrees when driftErrorRate = 1.")]
    [Range(0f, 45f)] public float maxYawDriftDeg = 10f;
    [Min(0.1f)] public float driftOscillationHz = 0.35f;

    [Header("Speed wobble")]
    [Tooltip("Max speed multiplier wobble when speedErrorRate = 1 (e.g. 0.15 means ±15%).")]
    [Range(0f, 0.5f)] public float maxSpeedWobble = 0.15f;
    [Min(0.1f)] public float speedWobbleHz = 0.6f;

    public float GetYawDriftDegrees()
    {
        EnsureRobotStats();
        if (robotStats == null || !robotStats.hasSavedCalibrationResult) return 0f;
        float rate = robotStats.driftErrorRate;
        float osc = Mathf.Sin(Time.time * Mathf.PI * 2f * driftOscillationHz);
        return osc * maxYawDriftDeg * rate;
    }

    public float GetSpeedMultiplier()
    {
        EnsureRobotStats();
        if (robotStats == null || !robotStats.hasSavedCalibrationResult) return 1f;
        float rate = robotStats.speedErrorRate;
        float osc = Mathf.Sin(Time.time * Mathf.PI * 2f * speedWobbleHz);
        return 1f + (osc * maxSpeedWobble * rate);
    }

    private void EnsureRobotStats()
    {
        if (robotStats != null) return;

        if (miniGame1Manager == null)
        {
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();
        }

        if (miniGame1Manager != null)
        {
            robotStats = miniGame1Manager.RobotStats;
        }
    }
}

