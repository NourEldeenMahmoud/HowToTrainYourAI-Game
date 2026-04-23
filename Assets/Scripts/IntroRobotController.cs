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
    [Tooltip("Optional explicit objects to force-visible whenever robot POV is active.")]
    [SerializeField] private GameObject[] forceVisibleInstructionObjects;
    [Tooltip("Names of direct children of robotPovCanvasRoot to keep visible during intro.")]
    [SerializeField] private string[] robotPovKeepChildNames = { "Message Panel button", "TimerText", "Energy Bar", "Instruction Text", "Instruction Text (1)", "Instruction Texts", "Objective Texts" };
    [Tooltip("(Optional) Assign instead of using robotPovCanvasRoot + keepNames. Filled at runtime if empty.")]
    [SerializeField] private GameObject[] robotPovHideOnIntro;
    [Tooltip("Direct children of Robot POV root to keep hidden until MiniGame1 completes.")]
    [SerializeField] private string[] keepHiddenUntilMiniGame1CompletedNames = { "Movement Buttons" };
    [SerializeField] private GameObject[] robotPovHideUntilMiniGame1Completed;

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

        if (controlManager != null)
            controlManager.SetSwitchEnabled(false);

        ResolveRobotPovHideList();
        ResolveHideUntilMiniGame1CompletedList();
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
            bool isInstructionLike = child.gameObject.name.StartsWith("Instruction Text", System.StringComparison.OrdinalIgnoreCase);
            bool isObjectiveLike = child.gameObject.name.StartsWith("Objective Text", System.StringComparison.OrdinalIgnoreCase);
            if (!keepSet.Contains(child.gameObject.name) && !isInstructionLike && !isObjectiveLike)
                toHide.Add(child.gameObject);
        }
        robotPovHideOnIntro = toHide.ToArray();
    }

    private void ResolveHideUntilMiniGame1CompletedList()
    {
        if (robotPovHideUntilMiniGame1Completed != null && robotPovHideUntilMiniGame1Completed.Length > 0)
            return;

        if (robotPovCanvasRoot == null)
            return;

        var toHide = new List<GameObject>();
        foreach (string childName in keepHiddenUntilMiniGame1CompletedNames)
        {
            if (string.IsNullOrEmpty(childName))
                continue;

            Transform child = robotPovCanvasRoot.transform.Find(childName);
            if (child != null && !toHide.Contains(child.gameObject))
                toHide.Add(child.gameObject);
        }

        robotPovHideUntilMiniGame1Completed = toHide.ToArray();
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
            // Try the expected path first.
            Transform t = robotMessagesCanvas.transform.Find("Buttons/Button (1)");
            if (t != null) closeButton = t.GetComponent<Button>();
        }

        // Fallback: scan all buttons and find by label text containing "close".
        if (closeButton == null)
        {
            foreach (Button b in robotMessagesCanvas.GetComponentsInChildren<Button>(true))
            {
                TMP_Text lbl = b.GetComponentInChildren<TMP_Text>(true);
                if (lbl != null && lbl.text.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    closeButton = b;
                    break;
                }
            }
        }

        // Final fallback: use the last button (Configure is first, Close is last).
        if (closeButton == null)
        {
            Button[] btns = robotMessagesCanvas.GetComponentsInChildren<Button>(true);
            if (btns.Length >= 2)
                closeButton = btns[btns.Length - 1];
        }

        if (closeButton == null)
            Debug.LogWarning("[IntroRobotController] Could not find Close button in Robot Messages Canvas.", this);
    }

    private void OnEnable()
    {
        if (startMiniGame1 == null)
            startMiniGame1 = FindFirstObjectByType<StartMiniGame1OnRobotControl>();

        if (startMiniGame1 != null)
            startMiniGame1.SetStartOnRobotControl(false);

        if (controlManager != null)
        {
            controlManager.ControlStateChanged += OnControlStateChanged;
        }

        if (miniGame1Manager != null)
        {
            miniGame1Manager.MiniGameCompleted += OnMiniGame1Completed;

            if (miniGame1Manager.CurrentPhase == MiniGame1Manager.MiniGame1Phase.Completed)
            {
                ApplyPostMiniGame1UIState();
            }
        }
    }

    public void ResetIntroState()
    {
        hasTriggered = false;

        if (startMiniGame1 != null)
            startMiniGame1.SetStartOnRobotControl(false);

        if (robotMessagesCanvas != null)
            robotMessagesCanvas.SetActive(false);

        if (controlManager != null)
            controlManager.SetSwitchEnabled(false);

        if (robotMovement != null)
            robotMovement.SetMovementEnabled(false);

        ShowCursor(false);
    }

    private void OnDisable()
    {
        if (controlManager != null)
        {
            controlManager.ControlStateChanged -= OnControlStateChanged;
            controlManager.SetSwitchEnabled(true);
        }

        if (miniGame1Manager != null)
        {
            miniGame1Manager.MiniGameCompleted -= OnMiniGame1Completed;
        }
    }

    private void OnControlStateChanged(bool isPlayerControl)
    {
        if (!isPlayerControl)
            EnsureInstructionTextsVisible();

        if (isPlayerControl || hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        if (controlManager != null)
            controlManager.SetSwitchEnabled(false);

        // Hide all Robot POV elements except Message button, TimerText and Energy Bar.
        if (robotPovHideOnIntro != null)
        {
            foreach (GameObject go in robotPovHideOnIntro)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
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

        if (controlManager != null)
            controlManager.SetSwitchEnabled(false);

        ShowCursor(false);

        // Restore all hidden Robot POV elements.
        if (robotPovHideOnIntro != null)
        {
            foreach (GameObject go in robotPovHideOnIntro)
            {
                if (go != null)
                {
                    go.SetActive(true);
                }
            }
        }

        EnsureInstructionTextsVisible();

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

    }

    private void OnMiniGame1Completed(MiniGame1EvaluationResult result)
    {
        ApplyPostMiniGame1UIState();
    }

    private void ApplyPostMiniGame1UIState()
    {
        if (controlManager != null)
            controlManager.SetSwitchEnabled(true);

        if (robotPovHideUntilMiniGame1Completed == null)
            return;

        foreach (GameObject go in robotPovHideUntilMiniGame1Completed)
        {
            if (go != null)
                go.SetActive(true);
        }

        EnsureInstructionTextsVisible();
    }

    private void EnsureInstructionTextsVisible()
    {
        if (robotPovCanvasRoot == null)
        {
            GameObject found = GameObject.Find("Robot POV Canvas");
            if (found == null)
                return;
            robotPovCanvasRoot = found;
        }

        if (forceVisibleInstructionObjects != null)
        {
            for (int i = 0; i < forceVisibleInstructionObjects.Length; i++)
                EnsureVisibleWithParents(forceVisibleInstructionObjects[i]);
        }

        foreach (Transform child in robotPovCanvasRoot.transform)
        {
            if (child == null)
                continue;

            string name = child.gameObject.name;
            bool isInstructionLike = name.StartsWith("Instruction Text", System.StringComparison.OrdinalIgnoreCase);
            bool isObjectiveLike = name.StartsWith("Objective Text", System.StringComparison.OrdinalIgnoreCase);
            if (isInstructionLike || isObjectiveLike)
                EnsureVisibleWithParents(child.gameObject);

            SetInstructionLikeChildrenVisibleRecursive(child);
        }
    }

    private void SetInstructionLikeChildrenVisibleRecursive(Transform root)
    {
        if (root == null)
            return;

        foreach (Transform child in root)
        {
            if (child == null)
                continue;

            string name = child.gameObject.name;
            bool isInstructionLike = name.StartsWith("Instruction Text", System.StringComparison.OrdinalIgnoreCase);
            bool isObjectiveLike = name.StartsWith("Objective Text", System.StringComparison.OrdinalIgnoreCase);
            if (isInstructionLike || isObjectiveLike)
                EnsureVisibleWithParents(child.gameObject);

            SetInstructionLikeChildrenVisibleRecursive(child);
        }
    }

    private static void EnsureVisibleWithParents(GameObject target)
    {
        if (target == null)
            return;

        Transform current = target.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }
}
