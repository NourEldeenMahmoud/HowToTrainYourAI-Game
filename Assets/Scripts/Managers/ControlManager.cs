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
    /// <summary>When true (and controlling player), movement/look are blocked; Tab invokes <see cref="messageBlockingTabPressed"/> (e.g. close message).</summary>
    private bool messageBlocksPlayerControls;
    private event Action messageBlockingTabPressed;
    private bool lookSuppressed;
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
        if (isInputLocked)
        {
            return;
        }

        if (messageBlocksPlayerControls && isPlayerControlActive)
        {
            // Close the message then immediately switch to robot.
            messageBlockingTabPressed?.Invoke();
            ToggleControl();
            return;
        }

        ToggleControl();
    }

    public void AddMessageBlockingTabListener(Action listener)
    {
        messageBlockingTabPressed += listener;
    }

    public void RemoveMessageBlockingTabListener(Action listener)
    {
        messageBlockingTabPressed -= listener;
    }

    /// <summary>
    /// Blocks player movement and camera look while a message UI is open, and hides the Player HUD.
    /// Tab while blocking: closes the message then switches to robot.
    /// Does not use full <see cref="SetInputLocked"/> so result-screen lock stays separate.
    /// </summary>
    public void SetMessageBlocksPlayerControls(bool blocking)
    {
        if (messageBlocksPlayerControls == blocking)
        {
            return;
        }

        messageBlocksPlayerControls = blocking;

        if (playerUiRoot != null)
        {
            bool showPlayerHud = !blocking && !isInputLocked && isPlayerControlActive;
            playerUiRoot.SetActive(showPlayerHud);
        }

        SyncGameplayInputLookAndCursor();
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

        if (locked)
        {
            // Hide all POV UI while locked (result screen, message modal, etc.)
            if (podUiRoot != null) podUiRoot.SetActive(false);
            if (playerUiRoot != null) playerUiRoot.SetActive(false);

            if (podFxVolume != null) podFxVolume.weight = 0f;
            targetFxWeight = 0f;
        }
        else
        {
            // Restore HUD like SetControlState (pod only when not controlling player).
            if (playerUiRoot != null)
            {
                playerUiRoot.SetActive(isPlayerControlActive);
            }

            if (podUiRoot != null)
            {
                podUiRoot.SetActive(!isPlayerControlActive);
            }
        }

        SyncGameplayInputLookAndCursor();

        InputLockChanged?.Invoke(isInputLocked);
    }

    private void SyncGameplayInputLookAndCursor()
    {
        bool fullLock = isInputLocked;
        bool messageFreezesPlayer = messageBlocksPlayerControls && isPlayerControlActive;

        if (playerInput != null)
        {
            if (fullLock)
            {
                playerInput.enabled = false;
            }
            else if (isPlayerControlActive)
            {
                playerInput.enabled = true;
                ApplyPlayerMoveSprintFreeze(messageFreezesPlayer);
            }
            else
            {
                playerInput.enabled = false;
                ApplyPlayerMoveSprintFreeze(false);
            }
        }

        if (robotInput != null)
        {
            robotInput.enabled = !isPlayerControlActive && !fullLock;
        }

        bool shouldSuppressLook = fullLock || messageFreezesPlayer;
        EnsureCinemachineControllersCached();

        if (cachedCinemachineInputControllers != null && cachedCinemachineInputControllersEnabled != null)
        {
            if (shouldSuppressLook)
            {
                if (!lookSuppressed)
                {
                    for (int i = 0; i < cachedCinemachineInputControllers.Length; i++)
                    {
                        CinemachineInputAxisController c = cachedCinemachineInputControllers[i];
                        if (c == null) continue;

                        cachedCinemachineInputControllersEnabled[i] = c.enabled;
                    }

                    lookSuppressed = true;
                }

                // Keep forcing off every sync — Cinemachine may re-enable the component (e.g. Auto Enable Inputs).
                for (int i = 0; i < cachedCinemachineInputControllers.Length; i++)
                {
                    CinemachineInputAxisController c = cachedCinemachineInputControllers[i];
                    if (c == null) continue;

                    c.enabled = false;
                }
            }
            else if (lookSuppressed)
            {
                for (int i = 0; i < cachedCinemachineInputControllers.Length; i++)
                {
                    CinemachineInputAxisController c = cachedCinemachineInputControllers[i];
                    if (c == null) continue;

                    c.enabled = cachedCinemachineInputControllersEnabled[i];
                }

                lookSuppressed = false;
            }
        }

        if (fullLock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ApplyPlayerMoveSprintFreeze(bool freeze)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        InputActionMap map = playerInput.actions.FindActionMap("Player");
        if (map == null)
        {
            return;
        }

        InputAction move = map.FindAction("Move");
        InputAction sprint = map.FindAction("Sprint");

        if (freeze)
        {
            move?.Disable();
            sprint?.Disable();
        }
        else
        {
            move?.Enable();
            sprint?.Enable();
        }
    }

    private void EnsureCinemachineControllersCached()
    {
        if (cachedCinemachineInputControllers != null && cachedCinemachineInputControllers.Length > 0)
        {
            return;
        }

        // Include inactive objects. Re-query if the first pass ran before cameras existed (empty array was cached as non-null).
        cachedCinemachineInputControllers = UnityEngine.Object.FindObjectsOfType<CinemachineInputAxisController>(true);
        cachedCinemachineInputControllersEnabled = new bool[cachedCinemachineInputControllers.Length];
    }

    private void SetControlState(bool isPlayer)
    {
        isPlayerControlActive = isPlayer;

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

        SyncGameplayInputLookAndCursor();
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
