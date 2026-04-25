using UnityEngine;

/// <summary>
/// Temporary, mini-game-only fault injection. This is NOT a persistent robot update.
/// Enabled/parameterized by MiniGame1 challenges during calibration.
/// </summary>
public class MiniGame1FaultState : MonoBehaviour
{
    [Header("Enable")]
    public bool faultsEnabled = true;

    [Header("Drift (degrees)")]
    [Tooltip("Constant yaw drift applied to movement direction while enabled.")]
    [Range(-90f, 90f)] public float yawDriftDeg = 0f;

    [Header("Speed wobble")]
    [Tooltip("When > 0, speed multiplier oscillates by this amplitude (e.g. 0.15 => ±15%).")]
    [Range(0f, 0.5f)] public float speedWobbleAmplitude = 0f;
    [Min(0.1f)] public float speedWobbleHz = 0.8f;

    public void ClearAll()
    {
        yawDriftDeg = 0f;
        speedWobbleAmplitude = 0f;
    }

    public float GetSpeedMultiplier()
    {
        if (!faultsEnabled) return 1f;
        if (speedWobbleAmplitude <= 0f) return 1f;
        float osc = Mathf.Sin(Time.time * Mathf.PI * 2f * speedWobbleHz);
        return 1f + (osc * speedWobbleAmplitude);
    }
}

