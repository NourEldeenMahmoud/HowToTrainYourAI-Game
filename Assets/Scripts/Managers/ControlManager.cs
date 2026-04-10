using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using System.Collections;
using System;
using TMPro;

public class ControlManager : MonoBehaviour
{
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
    [SerializeField] private TMP_Text timeText;

    private bool isPlayerControlActive = true;
    private float targetFxWeight;
    private Coroutine fxDelayRoutine;

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

    public void ToggleControl()
    {
        SetControlState(!isPlayerControlActive);
    }

    private void SetControlState(bool isPlayer)
    {
        isPlayerControlActive = isPlayer;

        if (playerInput != null) playerInput.enabled = isPlayer;
        if (robotInput != null) robotInput.enabled = !isPlayer;

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

        if (podUiRoot != null)
            podUiRoot.SetActive(false);

        fxDelayRoutine = StartCoroutine(ApplyTransitionAfterDelay(isPlayer ? 0f : 1f, !isPlayer));
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
