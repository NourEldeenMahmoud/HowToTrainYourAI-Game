using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using System.Collections;
using System;
using TMPro;

public class ControlManager : MonoBehaviour
{
    public event Action<bool> ControlStateChanged;
    public event Action<bool> InputLockChanged;

    [Header("Input Components")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerInput robotInput;

    [Header("Global Input Action")]
    [SerializeField] private InputActionReference switchAction;

    [Header("Optional Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera robotCamera;
    [SerializeField] private int activeCameraPriority = 10;
    [SerializeField] private int inactiveCameraPriority = 0;

    [Header("Optional Pod FX")]
    [SerializeField] private Volume podFxVolume;
    [SerializeField, Min(0f)] private float fxStartDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float fxBlendSpeed = 6f;
    [SerializeField] private GameObject podUiRoot;

    [Header("Optional UI")]
    [Tooltip("Shown when player controls the character (hidden in robot/pod mode).")]
    [SerializeField] private GameObject playerUiRoot;
    [SerializeField] private TMP_Text timeText;

    private bool isPlayerControlActive = true;
    private bool isInputLocked;
    private CinemachineInputAxisController[] cachedCinemachineInputControllers;
    private bool[] cachedCinemachineInputControllersEnabled;
    private float targetFxWeight;
    private Coroutine fxDelayRoutine;
    private MiniGame1Manager miniGame1Manager;
    private CinemachineOrbitalFollow robotCameraOrbitalFollow;
    private float lastRobotCameraOffset;

    private void Awake()
    {
        SetControlState(true);

        // Force initial visual state to player mode.
        if (podFxVolume != null)
        {
            podFxVolume.weight = 0f;
        }
    }

    private void OnEnable()
    {
        if (switchAction != null && switchAction.action != null)
        {
            switchAction.action.performed += OnSwitchPerformed;
            switchAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fxDelayRoutine != null)
        {
            StopCoroutine(fxDelayRoutine);
            fxDelayRoutine = null;
        }

        if (switchAction != null && switchAction.action != null)
        {
            switchAction.action.performed -= OnSwitchPerformed;
            switchAction.action.Disable();
        }
    }

    private void OnSwitchPerformed(InputAction.CallbackContext context)
    {
        ToggleControl();
    }

    private void Update()
    {
        UpdateTimeText();
        ApplyRobotCameraInstability();

        if (podFxVolume == null)
        {
            return;
        }

        podFxVolume.weight = Mathf.MoveTowards(
            podFxVolume.weight,
            targetFxWeight,
            fxBlendSpeed * Time.deltaTime
        );
    }

    private void ApplyRobotCameraInstability()
    {
        if (robotCamera == null)
        {
            return;
        }

        if (robotCameraOrbitalFollow == null)
        {
            robotCameraOrbitalFollow = robotCamera.GetComponent<CinemachineOrbitalFollow>();
            if (robotCameraOrbitalFollow == null)
            {
                return;
            }
        }

        if (miniGame1Manager == null)
        {
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();
        }

        RobotStatsSO robotStats = miniGame1Manager != null ? miniGame1Manager.RobotStats : null;
        bool shouldApply =
            robotStats != null &&
            robotStats.hasSavedCalibrationResult &&
            !isPlayerControlActive &&
            !isInputLocked;

        float nextOffset = 0f;
        if (shouldApply)
        {
            nextOffset = Mathf.Sin(Time.time * Mathf.PI * 2f * 0.35f) * 10f * robotStats.cameraErrorRate;
        }

        float currentValueWithoutPreviousOffset = robotCameraOrbitalFollow.VerticalAxis.Value - lastRobotCameraOffset;
        robotCameraOrbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            currentValueWithoutPreviousOffset + nextOffset,
            robotCameraOrbitalFollow.VerticalAxis.Range.x,
            robotCameraOrbitalFollow.VerticalAxis.Range.y
        );

        lastRobotCameraOffset = nextOffset;
    }

    public void ToggleControl()
    {
        if (isInputLocked) return;
        SetControlState(!isPlayerControlActive);
    }

    public bool IsPlayerControlActive => isPlayerControlActive;
    public bool IsInputLocked => isInputLocked;

    public void SetInputLocked(bool locked)
    {
        if (isInputLocked == locked) return;
        isInputLocked = locked;

        if (playerInput != null) playerInput.enabled = !locked && isPlayerControlActive;
        if (robotInput != null) robotInput.enabled = !locked && !isPlayerControlActive;

        // Disable/enable Cinemachine look input controllers so mouse look stops while UI overlays are up.
        CacheCinemachineControllersIfNeeded();
        if (cachedCinemachineInputControllers != null && cachedCinemachineInputControllersEnabled != null)
        {
            for (int i = 0; i < cachedCinemachineInputControllers.Length; i++)
            {
                CinemachineInputAxisController c = cachedCinemachineInputControllers[i];
                if (c == null) continue;

                if (locked)
                {
                    cachedCinemachineInputControllersEnabled[i] = c.enabled;
                    c.enabled = false;
                }
                else
                {
                    c.enabled = cachedCinemachineInputControllersEnabled[i];
                }
            }
        }

        // Cursor should be usable when locked for UI clicks.
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;

        // Hide all POV UI while locked (result screen, etc.)
        if (podUiRoot != null) podUiRoot.SetActive(false);
        if (playerUiRoot != null) playerUiRoot.SetActive(false);

        // Keep FX off when locked.
        if (podFxVolume != null) podFxVolume.weight = 0f;
        targetFxWeight = 0f;

        InputLockChanged?.Invoke(isInputLocked);
    }

    private void CacheCinemachineControllersIfNeeded()
    {
        if (cachedCinemachineInputControllers != null) return;

        // Include inactive objects (result screen flow may disable UI/cameras).
        // Use older API for broader Unity compatibility.
        cachedCinemachineInputControllers = UnityEngine.Object.FindObjectsOfType<CinemachineInputAxisController>(true);
        cachedCinemachineInputControllersEnabled = new bool[cachedCinemachineInputControllers.Length];
    }

    private void SetControlState(bool isPlayer)
    {
        isPlayerControlActive = isPlayer;

        if (playerInput != null) playerInput.enabled = !isInputLocked && isPlayer;
        if (robotInput != null) robotInput.enabled = !isInputLocked && !isPlayer;

        if (playerCamera != null)
            playerCamera.Priority = isPlayer ? activeCameraPriority : inactiveCameraPriority;
        if (robotCamera != null)
            robotCamera.Priority = isPlayer ? inactiveCameraPriority : activeCameraPriority;

        // Keep FX off during camera/control transition, then blend in/out after delay.
        if (fxDelayRoutine != null)
        {
            StopCoroutine(fxDelayRoutine);
        }

        if (podFxVolume != null)
        {
            podFxVolume.weight = 0f;
        }

        if (playerUiRoot != null)
            playerUiRoot.SetActive(!isInputLocked && isPlayer);

        if (podUiRoot != null)
            podUiRoot.SetActive(false);

        if (!isInputLocked)
            fxDelayRoutine = StartCoroutine(ApplyTransitionAfterDelay(isPlayer ? 0f : 1f, !isPlayer));

        ControlStateChanged?.Invoke(isPlayerControlActive);
    }

    private IEnumerator ApplyTransitionAfterDelay(float nextTargetWeight, bool showPodUi)
    {
        if (fxStartDelay > 0f)
        {
            yield return new WaitForSeconds(fxStartDelay);
        }

        targetFxWeight = nextTargetWeight;

        if (podUiRoot != null)
            podUiRoot.SetActive(showPodUi);

        fxDelayRoutine = null;
    }

    private void UpdateTimeText()
    {
        if (timeText == null)
        {
            return;
        }

        timeText.text = DateTime.Now.ToString("HH:mm:ss");
    }
}
