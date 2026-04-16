using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the intro sequence before the first mini-game:
/// - When the player first switches to robot control, hides most of the Robot POV UI
///   (keeping only the Message button, TimerText and Energy Bar) and disables robot movement.
/// - Opening the Robot Messages Canvas shows a "configure movement first" message with a "Configure" button.
/// - Pressing Configure restores the full POV UI, re-enables robot movement and starts the mini-game.
///
/// All Inspector references have runtime fallbacks so the component works without manual wiring.
/// </summary>
public class IntroRobotController : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private MiniGame1Manager miniGame1Manager;
    [SerializeField] private RobotMovement robotMovement;
    [Tooltip("Disable this component so it no longer auto-starts the mini-game on robot control.")]
    [SerializeField] private StartMiniGame1OnRobotControl startMiniGame1;

    [Header("Robot POV — assign the canvas root; children not in keepNames will be hidden")]
    [Tooltip("Root of the Robot POV Canvas. If null, searches by name 'Robot POV Canvas'.")]
    [SerializeField] private GameObject robotPovCanvasRoot;
    [Tooltip("Names of direct children of robotPovCanvasRoot to keep visible during intro.")]
    [SerializeField] private string[] robotPovKeepChildNames = { "Message Panel button", "TimerText", "Energy Bar", "Instruction Text (1)" };
    [Tooltip("(Optional) Assign instead of using robotPovCanvasRoot + keepNames. Filled at runtime if empty.")]
    [SerializeField] private GameObject[] robotPovHideOnIntro;

    [Header("Message Panel Button (auto-found by name if null)")]
    [Tooltip("The 'Message Panel button' in Robot POV Canvas. Auto-found by name if null.")]
    [SerializeField] private Button messagePanelButton;

    [Header("Robot Messages Canvas (auto-found by name if null)")]
    [Tooltip("Root of the Robot Messages Canvas. If null, searches by name 'Robot Messages Canvas'.")]
    [SerializeField] private GameObject robotMessagesCanvas;
    [Tooltip("TMP_Text body inside the canvas. Auto-found by tag 'MessageBody' or first TMP_Text in canvas.")]
    [SerializeField] private TMP_Text messageBodyText;
    [Tooltip("The Continue/Configure button (path: Buttons/Button). Auto-found if null.")]
    [SerializeField] private Button configureButton;
    [Tooltip("TMP_Text label of configureButton. Auto-found as first TMP_Text child of configureButton.")]
    [SerializeField] private TMP_Text configureButtonLabel;
    [Tooltip("The Close button (path: Buttons/Button (1)). Auto-found if null.")]
    [SerializeField] private Button closeButton;

    [Header("Intro Texts")]
    [SerializeField, TextArea(3, 6)]
    private string introBodyText = "لازم تظبط اعدادات الموفمنت الاول عشان تقدر تشوف الرساله";
    [SerializeField] private string configureBtnText = "Configure";

    private bool hasTriggered;

    private void Awake()
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        if (miniGame1Manager == null)
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();

        if (robotMovement == null)
            robotMovement = FindFirstObjectByType<RobotMovement>();

        if (startMiniGame1 == null)
            startMiniGame1 = FindFirstObjectByType<StartMiniGame1OnRobotControl>();

        // Prevent auto-start; we control when the mini-game begins.
        if (startMiniGame1 != null)
            startMiniGame1.SetStartOnRobotControl(false);

        ResolveRobotPovHideList();
        ResolveMessagePanelButton();
        ResolveRobotMessagesCanvasRefs();
    }

    private void ResolveRobotPovHideList()
    {
        if (robotPovHideOnIntro != null && robotPovHideOnIntro.Length > 0)
            return; // Already assigned in Inspector.

        if (robotPovCanvasRoot == null)
        {
            GameObject found = GameObject.Find("Robot POV Canvas");
            if (found == null)
            {
                Debug.LogWarning("[IntroRobotController] Could not find 'Robot POV Canvas' in scene.", this);
                return;
            }
            robotPovCanvasRoot = found;
        }

        var keepSet = new HashSet<string>(robotPovKeepChildNames);
        var toHide = new List<GameObject>();
        foreach (Transform child in robotPovCanvasRoot.transform)
        {
            if (!keepSet.Contains(child.gameObject.name))
                toHide.Add(child.gameObject);
        }
        robotPovHideOnIntro = toHide.ToArray();
    }

    private void ResolveMessagePanelButton()
    {
        if (messagePanelButton != null || robotPovCanvasRoot == null)
            return;

        Transform btnTransform = robotPovCanvasRoot.transform.Find("Message Panel button");
        if (btnTransform != null)
            messagePanelButton = btnTransform.GetComponent<Button>();

        if (messagePanelButton == null)
            Debug.LogWarning("[IntroRobotController] Could not find 'Message Panel button' in Robot POV Canvas.", this);
    }

    private void ResolveRobotMessagesCanvasRefs()
    {
        if (robotMessagesCanvas == null)
        {
            robotMessagesCanvas = GameObject.Find("Robot Messages Canvas");
            if (robotMessagesCanvas == null)
            {
                Debug.LogWarning("[IntroRobotController] Could not find 'Robot Messages Canvas' in scene.", this);
                return;
            }
        }

        if (messageBodyText == null)
        {
            // The body text in the Robot Messages Canvas Texts prefab is named "Title Text (3)".
            Transform bodyTransform = robotMessagesCanvas.transform.Find("Texts/Title Text (3)");
            if (bodyTransform != null)
                messageBodyText = bodyTransform.GetComponent<TMP_Text>();

            // Fallback: find the largest TMP_Text that is not inside a Button.
            if (messageBodyText == null)
            {
                TMP_Text best = null;
                float bestSize = -1f;
                foreach (TMP_Text t in robotMessagesCanvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.GetComponentInParent<Button>(true) != null)
                        continue;
                    RectTransform rt = t.GetComponent<RectTransform>();
                    float area = rt != null ? rt.rect.width * rt.rect.height : 0f;
                    if (area > bestSize)
                    {
                        bestSize = area;
                        best = t;
                    }
                }
                messageBodyText = best;
            }
        }

        if (configureButton == null)
        {
            Transform t = robotMessagesCanvas.transform.Find("Buttons/Button");
            if (t != null) configureButton = t.GetComponent<Button>();
        }
        if (configureButton == null)
            configureButton = robotMessagesCanvas.GetComponentInChildren<Button>(true);

        if (configureButtonLabel == null && configureButton != null)
            configureButtonLabel = configureButton.GetComponentInChildren<TMP_Text>(true);

        if (closeButton == null)
        {
            Transform t = robotMessagesCanvas.transform.Find("Buttons/Button (1)");
            if (t != null) closeButton = t.GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
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
        if (isPlayerControl || hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        // Hide all Robot POV elements except Message button, TimerText and Energy Bar.
        foreach (GameObject go in robotPovHideOnIntro)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }

        // Disable robot movement so the player cannot move the robot yet.
        if (robotMovement != null)
        {
            robotMovement.SetMovementEnabled(false);
        }

        // Wire the Message Panel button to open the Robot Messages Canvas.
        if (messagePanelButton != null)
        {
            messagePanelButton.onClick.RemoveAllListeners();
            messagePanelButton.onClick.AddListener(OnMessagePanelButtonClicked);
        }

        // Prepare the Robot Messages Canvas with the intro text and Configure button.
        if (messageBodyText != null)
            messageBodyText.text = introBodyText;

        if (configureButtonLabel != null)
            configureButtonLabel.text = configureBtnText;

        if (configureButton != null)
        {
            configureButton.onClick.RemoveAllListeners();
            configureButton.onClick.AddListener(OnConfigureClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    private void OnMessagePanelButtonClicked()
    {
        if (robotMessagesCanvas != null)
        {
            robotMessagesCanvas.SetActive(true);
            ShowCursor(true);
        }
    }

    private void OnCloseButtonClicked()
    {
        if (robotMessagesCanvas != null)
            robotMessagesCanvas.SetActive(false);

        ShowCursor(false);
    }

    private void ShowCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }

    public void OnConfigureClicked()
    {
        if (robotMessagesCanvas != null)
            robotMessagesCanvas.SetActive(false);

        ShowCursor(false);

        // Restore all hidden Robot POV elements.
        foreach (GameObject go in robotPovHideOnIntro)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }

        // Re-enable robot movement now that calibration is about to start.
        if (robotMovement != null)
        {
            robotMovement.SetMovementEnabled(true);
        }

        // Start the mini-game (calibration sequence).
        if (miniGame1Manager != null)
        {
            miniGame1Manager.StartMiniGame();
        }

        // This controller has done its job.
        enabled = false;
    }
}
