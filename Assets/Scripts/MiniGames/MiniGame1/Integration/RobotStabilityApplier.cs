using UnityEngine;
using Unity.Cinemachine;

public class RobotStabilityApplier : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RobotStatsSO robotStats;
    [SerializeField] private RobotMovement robotMovement;
    [SerializeField] private CinemachineCamera robotCamera;
    private MiniGame1Manager miniGame1Manager;
    private MiniGame2Manager miniGame2Manager;
    private ControlManager controlManager;
    private CinemachineOrbitalFollow robotCameraOrbitalFollow;

    [Header("Fault Roll Timing")]
    [Tooltip("Minimum seconds between random post-MG1 fault checks.")]
    [SerializeField, Min(0.1f)] private float minFaultCheckIntervalSeconds = 10f;
    [Tooltip("Maximum seconds between random post-MG1 fault checks.")]
    [SerializeField, Min(0.1f)] private float maxFaultCheckIntervalSeconds = 18f;
    [Tooltip("Scales all post-MG1 fault trigger chances (0.5 = half as frequent).")]
    [SerializeField, Range(0f, 1f)] private float faultChanceMultiplier = 0.5f;
    [Tooltip("After any fault triggers, pause new fault rolls for this many seconds.")]
    [SerializeField, Min(0f)] private float postFaultGracePeriodSeconds = 8f;

    [Header("Debug")]
    [SerializeField] private bool enableFaultLogging = true;

    [Header("Drift Fault Event")]
    [Tooltip("Yaw drift applied while a post-MG1 drift fault event is active.")]
    [SerializeField, Range(0f, 45f)] private float driftFaultYawDeg = 10f;
    [Tooltip("How long a triggered drift fault lasts.")]
    [SerializeField, Min(0.1f)] private float driftFaultDurationSeconds = 3f;

    [Header("Speed Fault Event")]
    [Tooltip("How long sprint is blocked when a speed fault triggers.")]
    [SerializeField, Min(0.1f)] private float sprintBlockDurationSeconds = 1.5f;

    [Header("Camera Fault Event")]
    [Tooltip("Pitch offset applied while a post-MG1 camera fault event is active.")]
    [SerializeField, Range(0f, 60f)] private float cameraFaultPitchOffsetDeg = 10f;
    [Tooltip("How long a triggered camera pitch fault lasts.")]
    [SerializeField, Min(0.1f)] private float cameraFaultDurationSeconds = 2.5f;

    private float nextDriftRollTime;
    private float driftFaultEndTime;
    private float activeDriftYawDeg;
    private float nextSpeedRollTime;
    private float nextCameraRollTime;
    private float cameraFaultEndTime;
    private float activeCameraFaultOffset;
    private float lastCameraFaultOffset;
    private bool persistentFaultsWereAvailable;
    private float nextAnyFaultAllowedTime;

    private void Awake()
    {
        if (robotMovement == null)
            robotMovement = GetComponent<RobotMovement>();

        ScheduleNextDriftRoll();
        ScheduleNextSpeedRoll();
        ScheduleNextCameraRoll();
    }

    private void Update()
    {
        EnsureRobotStats();
        bool faultsAvailable = ArePersistentFaultsAvailable();
        if (faultsAvailable && !persistentFaultsWereAvailable)
        {
            ScheduleNextDriftRoll();
            ScheduleNextSpeedRoll();
        }

        persistentFaultsWereAvailable = faultsAvailable;
        if (!faultsAvailable)
        {
            activeDriftYawDeg = 0f;
            driftFaultEndTime = 0f;
            ClearCameraFaultOffset();
            return;
        }

        UpdateDriftFaultEvent();
        UpdateSpeedFaultEvent();
        UpdateCameraFaultEvent();
    }

    public float GetYawDriftDegrees()
    {
        if (!ArePersistentFaultsAvailable()) return 0f;
        return Time.time < driftFaultEndTime ? activeDriftYawDeg : 0f;
    }

    private void UpdateDriftFaultEvent()
    {
        if (!IsRobotControlAvailable())
            return;

        if (Time.time < nextAnyFaultAllowedTime)
            return;

        if (Time.time < nextDriftRollTime || Time.time < driftFaultEndTime)
            return;

        float driftChance = Mathf.Clamp01(robotStats.driftErrorRate * faultChanceMultiplier);
        if (UnityEngine.Random.value < driftChance)
        {
            float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            activeDriftYawDeg = driftFaultYawDeg * sign;
            driftFaultEndTime = Time.time + driftFaultDurationSeconds;
            nextAnyFaultAllowedTime = Time.time + Mathf.Max(0f, postFaultGracePeriodSeconds);
            LogFault($"Drift fault triggered: yaw={activeDriftYawDeg:F1}deg duration={driftFaultDurationSeconds:F1}s chance={driftChance:F2}");
        }

        ScheduleNextDriftRoll();
    }

    private void UpdateSpeedFaultEvent()
    {
        if (!IsRobotControlAvailable())
            return;

        if (Time.time < nextAnyFaultAllowedTime)
            return;

        if (Time.time < nextSpeedRollTime)
            return;

        float speedChance = Mathf.Clamp01(robotStats.speedErrorRate * faultChanceMultiplier);
        if (UnityEngine.Random.value < speedChance)
        {
            if (robotMovement == null)
                robotMovement = GetComponent<RobotMovement>();

            if (robotMovement != null)
            {
                robotMovement.CancelSprintFromFault(sprintBlockDurationSeconds);
                nextAnyFaultAllowedTime = Time.time + Mathf.Max(0f, postFaultGracePeriodSeconds);
                LogFault($"Speed fault triggered: sprint blocked for {sprintBlockDurationSeconds:F1}s chance={speedChance:F2}");
            }
        }

        ScheduleNextSpeedRoll();
    }

    private void ScheduleNextDriftRoll()
    {
        nextDriftRollTime = Time.time + GetRandomFaultInterval();
    }

    private void ScheduleNextSpeedRoll()
    {
        nextSpeedRollTime = Time.time + GetRandomFaultInterval();
    }

    private void UpdateCameraFaultEvent()
    {
        if (!ResolveCameraReferences())
            return;

        bool shouldApply = ArePersistentFaultsAvailable() && IsRobotControlAvailable();
        if (shouldApply && Time.time >= nextAnyFaultAllowedTime && Time.time >= nextCameraRollTime && Time.time >= cameraFaultEndTime)
        {
            float cameraChance = Mathf.Clamp01(robotStats.cameraErrorRate * faultChanceMultiplier);
            if (UnityEngine.Random.value < cameraChance)
            {
                float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                activeCameraFaultOffset = cameraFaultPitchOffsetDeg * sign;
                cameraFaultEndTime = Time.time + cameraFaultDurationSeconds;
                nextAnyFaultAllowedTime = Time.time + Mathf.Max(0f, postFaultGracePeriodSeconds);
                LogFault($"Camera fault triggered: pitchOffset={activeCameraFaultOffset:F1}deg duration={cameraFaultDurationSeconds:F1}s chance={cameraChance:F2}");
            }

            ScheduleNextCameraRoll();
        }

        float nextOffset = shouldApply && Time.time < cameraFaultEndTime ? activeCameraFaultOffset : 0f;
        float valueWithoutPreviousOffset = robotCameraOrbitalFollow.VerticalAxis.Value - lastCameraFaultOffset;
        robotCameraOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            valueWithoutPreviousOffset + nextOffset,
            robotCameraOrbitalFollow.VerticalAxis.Range.x,
            robotCameraOrbitalFollow.VerticalAxis.Range.y
        );
        lastCameraFaultOffset = nextOffset;
    }

    private void ClearCameraFaultOffset()
    {
        if (Mathf.Abs(lastCameraFaultOffset) <= 0.0001f)
            return;

        if (!ResolveCameraReferences())
            return;

        robotCameraOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            robotCameraOrbitalFollow.VerticalAxis.Value - lastCameraFaultOffset,
            robotCameraOrbitalFollow.VerticalAxis.Range.x,
            robotCameraOrbitalFollow.VerticalAxis.Range.y
        );

        lastCameraFaultOffset = 0f;
        activeCameraFaultOffset = 0f;
        cameraFaultEndTime = 0f;
    }

    private void ScheduleNextCameraRoll()
    {
        nextCameraRollTime = Time.time + GetRandomFaultInterval();
    }

    private float GetRandomFaultInterval()
    {
        float min = Mathf.Max(0.1f, minFaultCheckIntervalSeconds);
        float max = Mathf.Max(min, maxFaultCheckIntervalSeconds);
        return UnityEngine.Random.Range(min, max);
    }

    private bool ArePersistentFaultsAvailable()
    {
        EnsureRobotStats();
        if (robotStats == null || !robotStats.hasSavedCalibrationResult)
            return false;

        if (miniGame1Manager == null)
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();

        if (miniGame2Manager == null)
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();

        bool miniGame1Running = miniGame1Manager != null && miniGame1Manager.IsMiniGameRunning;
        bool miniGame2Running = miniGame2Manager != null && miniGame2Manager.IsMiniGameRunning;
        return !miniGame1Running && !miniGame2Running;
    }

    private bool IsRobotControlAvailable()
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        return controlManager == null || (!controlManager.IsPlayerControlActive && !controlManager.IsInputLocked);
    }

    private bool ResolveCameraReferences()
    {
        if (robotCameraOrbitalFollow != null)
            return true;

        if (robotCamera == null)
        {
            if (controlManager == null)
                controlManager = FindFirstObjectByType<ControlManager>();

            if (controlManager != null)
                robotCamera = controlManager.RobotCamera;
        }

        if (robotCamera == null)
            return false;

        robotCameraOrbitalFollow = robotCamera.GetComponent<CinemachineOrbitalFollow>();
        return robotCameraOrbitalFollow != null;
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

    private void LogFault(string message)
    {
        if (!enableFaultLogging)
            return;

        Debug.Log($"[MG1][PostFault] {message}", this);
    }
}
