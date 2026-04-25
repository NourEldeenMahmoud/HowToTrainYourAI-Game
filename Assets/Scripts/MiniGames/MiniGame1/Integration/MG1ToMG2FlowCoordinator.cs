using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class MG1ToMG2FlowCoordinator : MonoBehaviour
{
    private enum FlowState
    {
        Idle,
        AwaitingMessageClose,
        GoToStorage,
        Transitioning,
        Completed
    }

    [Header("References")]
    [SerializeField] private MiniGame1ResultScreenUI miniGame1ResultScreenUI;
    [Tooltip("Optional. If empty, the coordinator chooses the active transmission UI by name/content.")]
    [SerializeField] private GameObject messageCanvasRoot;
    [SerializeField] private TMP_Text grandfatherMessageBodyText;
    [SerializeField] private ControlManager controlManager;

    [Header("Message")]
    [SerializeField, TextArea(5, 10)] private string grandfatherBrokenMessage =
        "[Signal restored... 36%]\n" +
        "If you are hearing this... head to the storage room immediately.\n" +
        "Do not trust the route marked on the m--- map.\n" +
        "The second key is hidden behind the old---\n" +
        "[AUDIO DROP]\n" +
        "...when the lights flicker, stop moving and wait.\n" +
        "I repeat, wait fo---\n" +
        "[Transmission lost]";

    [Header("Objective Prompt")]
    [SerializeField] private string storageObjectiveText = "Head to the storage room.";
    [SerializeField] private TMP_Text[] explicitRobotInstructionTexts;
    [SerializeField] private TMP_Text[] explicitPlayerInstructionTexts;

    [Header("Transition")]
    [SerializeField] private string miniGame2ScenePath = "Assets/Scenes/Oraby/Second MiniGame.unity";
    [SerializeField, Min(0.05f)] private float fadeOutDuration = 1.0f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Debug")]
    [SerializeField] private bool enableLogs = true;

    private FlowState state = FlowState.Idle;
    private bool transitionRequested;
    private CanvasGroup fadeCanvasGroup;
    private bool sawMessageCanvasOpen;
    private GameObject targetMessageUiRoot;
    private Button[] hookedCloseButtons;
    private bool robotPovWasVisibleBeforeMessage;
    private bool robotPovHiddenByMessage;

    public bool CanStartStorageDoorTransition => state == FlowState.GoToStorage && !transitionRequested;

    public void BeginAfterApplyImprovements()
    {
        OnApplyImprovementsConfirmed();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureCoordinatorAfterSceneLoad()
    {
        MiniGame1ResultScreenUI resultUi = Object.FindFirstObjectByType<MiniGame1ResultScreenUI>();
        if (resultUi == null)
            return;

        if (resultUi.GetComponent<MG1ToMG2FlowCoordinator>() == null)
            resultUi.gameObject.AddComponent<MG1ToMG2FlowCoordinator>();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (miniGame1ResultScreenUI != null)
            miniGame1ResultScreenUI.ApplyImprovementsClicked += OnApplyImprovementsConfirmed;

        HookMessageCloseButtons();
    }

    private void OnDisable()
    {
        if (miniGame1ResultScreenUI != null)
            miniGame1ResultScreenUI.ApplyImprovementsClicked -= OnApplyImprovementsConfirmed;

        UnhookMessageCloseButtons();
    }

    public bool TryStartStorageDoorTransition()
    {
        if (!CanStartStorageDoorTransition)
            return false;

        transitionRequested = true;
        StartCoroutine(FadeOutAndLoadMiniGame2());
        return true;
    }

    private void Update()
    {
        if (state != FlowState.AwaitingMessageClose)
            return;

        ApplyGrandfatherMessageText();
        ApplyPostMiniGameMessageButtons();

        GameObject observedUi = ResolveTargetMessageUiRoot();
        if (observedUi == null)
            return;

        bool isOpen = observedUi.activeInHierarchy;
        if (isOpen)
        {
            SetRobotPovHiddenForMessage(true);
            sawMessageCanvasOpen = true;
            return;
        }

        if (!sawMessageCanvasOpen)
            return;

        sawMessageCanvasOpen = false;
        OnMessageCanvasClosedByUi();
    }

    private void OnApplyImprovementsConfirmed()
    {
        if (state != FlowState.Idle)
            return;

        ResolveReferences();
        if (ResolveMessageBodyText() == null)
        {
            Debug.LogWarning("[MG1->MG2] Could not resolve the Message Canvas text body.", this);
            return;
        }

        grandfatherMessageBodyText = null;
        ApplyGrandfatherMessageText();
        HookMessageCloseButtons();
        ApplyPostMiniGameMessageButtons();
        UpdateReadCorruptedMessageObjectiveTexts();

        state = FlowState.AwaitingMessageClose;
        GameObject observedUi = ResolveTargetMessageUiRoot();
        sawMessageCanvasOpen = observedUi != null && observedUi.activeInHierarchy;
        Log("Apply confirmed -> waiting for player to open and close transmission message");
    }

    private void OnMessageCanvasClosedByUi()
    {
        if (state != FlowState.AwaitingMessageClose)
            return;

        state = FlowState.GoToStorage;
        SetRobotPovHiddenForMessage(false);
        UpdateStorageObjectiveTexts();
        Log("Grandfather message closed -> objective set to storage room");
    }

    private void SetRobotPovHiddenForMessage(bool hidden)
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        GameObject robotUiRoot = controlManager != null ? controlManager.RobotUiRoot : GameObject.Find("Robot POV Canvas");
        if (robotUiRoot == null)
            return;

        if (hidden)
        {
            if (robotPovHiddenByMessage)
                return;

            robotPovWasVisibleBeforeMessage = robotUiRoot.activeSelf;
            if (robotPovWasVisibleBeforeMessage)
                robotUiRoot.SetActive(false);

            robotPovHiddenByMessage = true;
            return;
        }

        if (!robotPovHiddenByMessage)
            return;

        robotPovHiddenByMessage = false;
        if (robotPovWasVisibleBeforeMessage && controlManager != null && !controlManager.IsPlayerControlActive)
            robotUiRoot.SetActive(true);
    }

    private void ApplyGrandfatherMessageText()
    {
        TMP_Text body = ResolveMessageBodyText();
        if (body != null)
            body.text = grandfatherBrokenMessage;
    }

    private TMP_Text ResolveMessageBodyText()
    {
        GameObject resolvedRoot = ResolveTargetMessageUiRoot();
        if (resolvedRoot != null)
            messageCanvasRoot = resolvedRoot;

        if (grandfatherMessageBodyText != null)
        {
            if (messageCanvasRoot == null || grandfatherMessageBodyText.transform.IsChildOf(messageCanvasRoot.transform))
                return grandfatherMessageBodyText;

            grandfatherMessageBodyText = null;
        }

        if (messageCanvasRoot != null)
        {
            Transform expectedInCanvas = FindChildByName(messageCanvasRoot.transform, "Title Text (3)");
            if (expectedInCanvas != null)
            {
                grandfatherMessageBodyText = expectedInCanvas.GetComponent<TMP_Text>();
                if (grandfatherMessageBodyText != null)
                    return grandfatherMessageBodyText;
            }

            TMP_Text[] underCanvas = messageCanvasRoot.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text bestCanvasText = SelectBestBodyText(underCanvas);
            if (bestCanvasText != null)
            {
                grandfatherMessageBodyText = bestCanvasText;
                return grandfatherMessageBodyText;
            }
        }

        return null;
    }

    private static TMP_Text SelectBestBodyText(TMP_Text[] texts)
    {
        if (texts == null)
            return null;

        TMP_Text best = null;
        float bestArea = -1f;
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text tmp = texts[i];
            if (tmp == null)
                continue;

            if (tmp.GetComponentInParent<Button>(true) != null)
                continue;

            if (!string.IsNullOrEmpty(tmp.text) &&
                tmp.text.IndexOf("CRITICAL", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return tmp;

            if (!string.IsNullOrEmpty(tmp.text) &&
                tmp.text.IndexOf("Interesting", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return tmp;

            if (!string.IsNullOrEmpty(tmp.text) &&
                tmp.text.IndexOf("Signal restored", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return tmp;

            RectTransform rect = tmp.GetComponent<RectTransform>();
            float area = rect != null ? rect.rect.width * rect.rect.height : 0f;
            if (area > bestArea)
            {
                bestArea = area;
                best = tmp;
            }
        }

        return best;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void UpdateStorageObjectiveTexts()
    {
        MG1InstructionSequenceController instructions = MG1InstructionSequenceController.Instance ?? FindFirstObjectByType<MG1InstructionSequenceController>();
        if (instructions != null)
        {
            instructions.SetHeadToStorageStage();
            return;
        }

        ApplyTextToExplicitList(explicitRobotInstructionTexts, storageObjectiveText);
        ApplyTextToExplicitList(explicitPlayerInstructionTexts, storageObjectiveText);

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        if (controlManager != null)
        {
            ApplyTextToInstructionLikeChildren(controlManager.RobotUiRoot, storageObjectiveText);
            ApplyTextToInstructionLikeChildren(controlManager.PlayerUiRoot, storageObjectiveText);
        }

        ApplyTextToInstructionLikeChildren(null, storageObjectiveText);
    }

    private void UpdateReadCorruptedMessageObjectiveTexts()
    {
        MG1InstructionSequenceController instructions = MG1InstructionSequenceController.Instance ?? FindFirstObjectByType<MG1InstructionSequenceController>();
        if (instructions != null)
            instructions.SetReadCorruptedMessageStage();
    }

    private static void ApplyTextToExplicitList(TMP_Text[] texts, string value)
    {
        if (texts == null)
            return;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text tmp = texts[i];
            if (tmp != null)
                tmp.text = value;
        }
    }

    private static void ApplyTextToInstructionLikeChildren(GameObject root, string value)
    {
        TMP_Text[] all = root != null
            ? root.GetComponentsInChildren<TMP_Text>(true)
            : Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp == null)
                continue;

            if (tmp.GetComponentInParent<Button>(true) != null || IsUnderNamedAncestor(tmp.transform, "Message Panel button"))
                continue;

            if (IsInteractionPromptText(tmp))
                continue;

            string n = tmp.gameObject.name;
            bool instructionLike = n.StartsWith("Instruction Text", System.StringComparison.OrdinalIgnoreCase);
            bool objectiveLike = n.StartsWith("Objective Text", System.StringComparison.OrdinalIgnoreCase);
            bool currentObjectiveText = !string.IsNullOrEmpty(tmp.text) &&
                (tmp.text.IndexOf("Reach The Target", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 tmp.text.IndexOf("Free Move", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 tmp.text.IndexOf("Drift", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 tmp.text.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 tmp.text.IndexOf("Speed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 tmp.text.IndexOf("Done", System.StringComparison.OrdinalIgnoreCase) >= 0);

            if (instructionLike || objectiveLike || currentObjectiveText)
                tmp.text = value;
        }
    }

    private IEnumerator FadeOutAndLoadMiniGame2()
    {
        state = FlowState.Transitioning;

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        if (controlManager != null)
        {
            controlManager.SetSwitchEnabled(false);
            controlManager.SetInputLocked(true);
        }

        RobotMovement robotMovement = FindFirstObjectByType<RobotMovement>();
        if (robotMovement != null)
            robotMovement.SetMovementEnabled(false);

        EnsureFadeCanvas();
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            fadeCanvasGroup.alpha = t;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        state = FlowState.Completed;

        Log("Loading scene: " + miniGame2ScenePath);
        LoadMiniGame2Scene();
    }

    private void LoadMiniGame2Scene()
    {
        if (SceneUtility.GetBuildIndexByScenePath(miniGame2ScenePath) >= 0)
        {
            SceneManager.LoadScene(miniGame2ScenePath);
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(miniGame2ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError(
            "MiniGame2 scene is not in the active Build Profile scene list: " + miniGame2ScenePath,
            this);
#endif
    }

    private void EnsureFadeCanvas()
    {
        if (fadeCanvasGroup != null)
            return;

        GameObject canvasGO = new GameObject("MG1ToMG2_FadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GraphicRaycaster raycaster = canvasGO.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        fadeCanvasGroup = canvasGO.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;

        GameObject imageGO = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageGO.GetComponent<Image>();
        image.color = fadeColor;
    }

    private void ResolveReferences()
    {
        if (miniGame1ResultScreenUI == null)
            miniGame1ResultScreenUI = GetComponent<MiniGame1ResultScreenUI>();

        if (miniGame1ResultScreenUI == null)
            miniGame1ResultScreenUI = FindFirstObjectByType<MiniGame1ResultScreenUI>();

        GameObject resolvedRoot = ResolveTargetMessageUiRoot();
        if (resolvedRoot != null)
            messageCanvasRoot = resolvedRoot;

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();
    }

    private GameObject ResolveTargetMessageUiRoot()
    {
        if (targetMessageUiRoot != null)
            return targetMessageUiRoot;

        GameObject best = FindMessageRootByContent();
        if (best != null)
        {
            targetMessageUiRoot = best;
            messageCanvasRoot = best;
            return targetMessageUiRoot;
        }

        best = GameObject.Find("Robot Messages Canvas");
        if (best == null)
            best = GameObject.Find("Message Canvas");

        if (best != null)
        {
            targetMessageUiRoot = best;
            messageCanvasRoot = best;
        }

        return targetMessageUiRoot;
    }

    private static GameObject FindMessageRootByContent()
    {
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text tmp = texts[i];
            if (tmp == null || string.IsNullOrEmpty(tmp.text))
                continue;

            bool looksLikeTransmissionBody =
                tmp.text.IndexOf("CRITICAL", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                tmp.text.IndexOf("Interesting", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                tmp.text.IndexOf("Signal restored", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!looksLikeTransmissionBody)
                continue;

            Canvas canvas = tmp.GetComponentInParent<Canvas>(true);
            if (canvas != null)
                return canvas.gameObject;
        }

        return null;
    }

    private static bool IsInteractionPromptText(TMP_Text tmp)
    {
        if (tmp == null || string.IsNullOrWhiteSpace(tmp.text))
            return false;

        string text = tmp.text;
        return text.IndexOf("Press", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
               (text.IndexOf("[E]", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf(" E", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsUnderNamedAncestor(Transform child, string ancestorName)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name.Equals(ancestorName, System.StringComparison.OrdinalIgnoreCase))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void HookMessageCloseButtons()
    {
        GameObject resolvedRoot = ResolveTargetMessageUiRoot();
        if (resolvedRoot != null)
            messageCanvasRoot = resolvedRoot;

        if (messageCanvasRoot == null)
            return;

        UnhookMessageCloseButtons();
        Button[] buttons = messageCanvasRoot.GetComponentsInChildren<Button>(true);
        var matches = new System.Collections.Generic.List<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text : button.gameObject.name;
            if (labelText.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            button.onClick.AddListener(OnMessageCanvasClosedByUi);
            matches.Add(button);
        }

        hookedCloseButtons = matches.ToArray();
    }

    private void ApplyPostMiniGameMessageButtons()
    {
        GameObject root = ResolveTargetMessageUiRoot();
        if (root == null)
            return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string labelText = label != null ? label.text : button.gameObject.name;
            bool isConfigure = labelText.IndexOf("configure", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (isConfigure)
                button.gameObject.SetActive(false);
        }
    }

    private void UnhookMessageCloseButtons()
    {
        if (hookedCloseButtons == null)
            return;

        for (int i = 0; i < hookedCloseButtons.Length; i++)
        {
            if (hookedCloseButtons[i] != null)
                hookedCloseButtons[i].onClick.RemoveListener(OnMessageCanvasClosedByUi);
        }

        hookedCloseButtons = null;
    }

    private void Log(string message)
    {
        if (enableLogs)
            Debug.Log("[MG1->MG2] " + message, this);
    }
}
