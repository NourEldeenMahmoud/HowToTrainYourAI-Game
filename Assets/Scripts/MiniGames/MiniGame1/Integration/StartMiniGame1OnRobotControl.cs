using UnityEngine;

public class StartMiniGame1OnRobotControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private MiniGame1Manager miniGame1Manager;

    [Header("Behavior")]
    [Tooltip("Start calibration when switching to robot control.")]
    [SerializeField] private bool startOnRobotControl = true;
    [Tooltip("Only trigger once per scene load.")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    public void SetStartOnRobotControl(bool value)
    {
        startOnRobotControl = value;
    }

    private void OnEnable()
    {
        if (controlManager == null)
        {
            controlManager = FindFirstObjectByType<ControlManager>();
        }

        if (miniGame1Manager == null)
        {
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();
        }

        if (controlManager != null)
        {
            controlManager.ControlStateChanged += OnControlStateChanged;
        }
    }

    private void OnDisable()
    {
        if (controlManager != null)
        {
            controlManager.ControlStateChanged -= OnControlStateChanged;
        }
    }

    private void OnControlStateChanged(bool isPlayerControl)
    {
        if (!startOnRobotControl) return;
        if (isPlayerControl) return;

        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;

        if (miniGame1Manager != null)
        {
            miniGame1Manager.StartMiniGame();
        }
    }
}

