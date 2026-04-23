using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MiniGame1ResultScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame1Manager miniGame1Manager;
    [Tooltip("Root of the result screen UI (e.g. Result Screen Canvas). Can be inactive at startup.")]
    [SerializeField] private GameObject resultScreenRoot;
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private GameObject robotPovUiRoot;

    [Tooltip("If true, missing UI refs are filled from the result prefab by name (same as before).")]
    [SerializeField] private bool autoFindOnEnable = true;

    [Header("UI - Texts")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text driftScoreText;
    [SerializeField] private TMP_Text cameraScoreText;
    [SerializeField] private TMP_Text speedScoreText;

    [Header("UI - Buttons")]
    [SerializeField] private Button applyImprovementsButton;
    [SerializeField] private Button recalibrateButton;

    [Header("UI - Bars (Scrollbars)")]
    [SerializeField] private Scrollbar speedBar;
    [SerializeField] private Scrollbar cameraBar;
    [SerializeField] private Scrollbar driftBar;

    [Header("Behavior")]
    [Tooltip("While the result screen is visible, disable player/robot inputs and hide POV UI.")]
    [SerializeField] private bool lockControlsWhileVisible = true;

    [Header("Debug")]
    [Tooltip("Logs which Scrollbar is bound to which row, and their hierarchy order.")]
    [SerializeField] private bool logBarBinding = false;

    private bool isResultVisible;
    private bool robotPovWasVisibleBeforeResult;

    private void OnEnable()
    {
        if (resultScreenRoot == null)
            resultScreenRoot = gameObject;

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        if (miniGame1Manager == null)
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();

        if (autoFindOnEnable)
            AutoFind();

        if (miniGame1Manager != null)
        {
            miniGame1Manager.MiniGameCompleted += OnMiniGameCompleted;

            if (miniGame1Manager.CurrentPhase == MiniGame1Manager.MiniGame1Phase.Completed)
                OnMiniGameCompleted(miniGame1Manager.LastResult);
        }

        if (applyImprovementsButton != null)
            applyImprovementsButton.onClick.AddListener(OnApplyImprovementsClicked);

        if (recalibrateButton != null)
            recalibrateButton.onClick.AddListener(OnRecalibrateClicked);
    }

    private void OnDisable()
    {
        if (miniGame1Manager != null)
            miniGame1Manager.MiniGameCompleted -= OnMiniGameCompleted;

        if (applyImprovementsButton != null)
            applyImprovementsButton.onClick.RemoveListener(OnApplyImprovementsClicked);

        if (recalibrateButton != null)
            recalibrateButton.onClick.RemoveListener(OnRecalibrateClicked);
    }

    private void OnMiniGameCompleted(MiniGame1EvaluationResult result)
    {
        isResultVisible = true;
        HideRobotPovWhileResultVisible();

        SetResultScreenVisible(true);

        if (autoFindOnEnable && (applyImprovementsButton == null || recalibrateButton == null))
        {
            AutoFind();
            RebindButtons();
        }

        EnsureEventSystemExists();

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(true);

        ApplyResult(result);
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    public void ApplyResult(MiniGame1EvaluationResult r)
    {
        if (finalScoreText != null)
            finalScoreText.text = $"{Mathf.RoundToInt(r.finalScore)}%";

        if (tierText != null)
            tierText.text = r.tier.ToString();

        if (driftScoreText != null) driftScoreText.text = $"{Mathf.RoundToInt(r.challengeScores.driftScore)}%";
        if (cameraScoreText != null) cameraScoreText.text = $"{Mathf.RoundToInt(r.challengeScores.cameraScore)}%";
        if (speedScoreText != null) speedScoreText.text = $"{Mathf.RoundToInt(r.challengeScores.speedScore)}%";

        // Bars are implemented as Scrollbars in the imported UI prefab.
        // IMPORTANT: This prefab uses a big default Handle Size, so changing only "value"
        // makes even low scores look large. We treat the handle as a progress fill:
        // - size = normalized score (0..1)
        // - value = 1 (pin fill to the right/end)
        // Row order in the imported prefab (top -> bottom):
        // Drift Handling, Steering Precision, Speed Control
        ApplyBar(driftBar, r.challengeScores.driftScore);
        ApplyBar(cameraBar, r.challengeScores.cameraScore);
        ApplyBar(speedBar, r.challengeScores.speedScore);

        if (logBarBinding)
        {
            LogBar("DRIFT", driftBar, r.challengeScores.driftScore);
            LogBar("STEERING", cameraBar, r.challengeScores.cameraScore);
            LogBar("SPEED", speedBar, r.challengeScores.speedScore);
        }
    }

    private static void ApplyBar(Scrollbar bar, float score0To100)
    {
        if (bar == null) return;
        float t = Mathf.Clamp01(score0To100 / 100f);

        // We want this to behave like a read-only progress bar:
        // - show fill left -> right matching the percentage
        // - do not respond to mouse/keyboard dragging/clicking
        //
        // NOTE: We must NOT disable the Scrollbar component itself. If we do, it stops
        // updating the handleRect anchors based on (size, value), so the visual fill
        // stops reflecting the percentage. Instead, we make it non-interactive.

        // 1. Non-interactive, but do not tint to DisabledColor.
        bar.transition = Selectable.Transition.None;
        bar.interactable = false;
        Navigation nav = bar.navigation;
        nav.mode = Navigation.Mode.None;
        bar.navigation = nav;

        // 2. Make the Scrollbar ignore pointer input entirely (safety on top of interactable=false).
        Graphic barGraphic = bar.GetComponent<Graphic>();
        if (barGraphic != null) barGraphic.raycastTarget = false;
        if (bar.handleRect != null)
        {
            Graphic handleGraphic = bar.handleRect.GetComponent<Graphic>();
            if (handleGraphic != null) handleGraphic.raycastTarget = false;

            // The Scrollbar recomputes handleRect anchors each frame from (value, size).
            // If sizeDelta/anchoredPosition are non-zero, the handle renders larger/offset
            // than the anchors define -> the fill width won't match the percentage.
            // Force anchors to be the single source of truth for the handle's visual size.
            bar.handleRect.sizeDelta = Vector2.zero;
            bar.handleRect.anchoredPosition = Vector2.zero;
        }

        // 3. Fill left -> right. Scrollbar will compute handleRect anchors:
        //    anchorMin.x = value * (1 - size), anchorMax.x = value * (1 - size) + size
        //    With value=0 and size=t, the handle covers exactly the first t% of the sliding area.
        bar.direction = Scrollbar.Direction.LeftToRight;
        bar.size = t;
        bar.value = 0f;
    }

    private static void LogBar(string label, Scrollbar bar, float score0To100)
    {
        if (bar == null)
        {
            Debug.Log($"{nameof(MiniGame1ResultScreenUI)}: {label} bar is NULL");
            return;
        }

        Transform t = bar.transform;
        string parentName = t.parent != null ? t.parent.name : "<no-parent>";
        int siblingIndex = t.GetSiblingIndex();
        Debug.Log(
            $"{nameof(MiniGame1ResultScreenUI)}: {label} -> '{t.name}' (parent '{parentName}', siblingIndex {siblingIndex}) score {Mathf.RoundToInt(score0To100)}%");
    }

    private void OnApplyImprovementsClicked()
    {
        isResultVisible = false;
        SetResultScreenVisible(false);

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(false);

        RestoreRobotPovAfterResult();
    }

    private void OnRecalibrateClicked()
    {
        if (miniGame1Manager != null)
            miniGame1Manager.StartMiniGame();

        isResultVisible = false;
        SetResultScreenVisible(false);

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(false);

        RestoreRobotPovAfterResult();
    }

    private void LateUpdate()
    {
        if (!isResultVisible || robotPovUiRoot == null)
            return;

        if (robotPovUiRoot.activeSelf)
            robotPovUiRoot.SetActive(false);
    }

    private void HideRobotPovWhileResultVisible()
    {
        if (robotPovUiRoot == null)
        {
            GameObject found = GameObject.Find("Robot POV Canvas");
            if (found != null)
                robotPovUiRoot = found;
        }

        if (robotPovUiRoot == null)
            return;

        robotPovWasVisibleBeforeResult = robotPovUiRoot.activeSelf;
        if (robotPovWasVisibleBeforeResult)
            robotPovUiRoot.SetActive(false);
    }

    private void RestoreRobotPovAfterResult()
    {
        if (robotPovUiRoot == null || !robotPovWasVisibleBeforeResult)
            return;

        if (controlManager != null && controlManager.IsPlayerControlActive)
            return;

        if (!robotPovUiRoot.activeSelf)
            robotPovUiRoot.SetActive(true);
    }

    private void SetResultScreenVisible(bool visible)
    {
        if (resultScreenRoot == null)
            return;

        if (!visible)
        {
            resultScreenRoot.SetActive(false);
            return;
        }

        if (!resultScreenRoot.activeSelf)
            resultScreenRoot.SetActive(true);

        Transform rootTransform = resultScreenRoot.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform child = rootTransform.GetChild(i);
            if (child != null && !child.gameObject.activeSelf)
                child.gameObject.SetActive(true);
        }
    }

    private void AutoFind()
    {
        if (miniGame1Manager == null)
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();

        Transform root = resultScreenRoot != null ? resultScreenRoot.transform : transform;

        if (finalScoreText == null)
            finalScoreText = FindTMPByName(root, "Instruction Text (2)");

        if (tierText == null)
            tierText = FindTMPByName(root, "Pass/Fail Text") ?? FindTMPContains(root, "Pass") ?? FindTMPContains(root, "Fail");

        if (driftScoreText == null) driftScoreText = FindTMPByName(root, "actual number (3)");
        if (cameraScoreText == null) cameraScoreText = FindTMPByName(root, "actual number (2)");
        if (speedScoreText == null) speedScoreText = FindTMPByName(root, "actual number (1)");

        if (applyImprovementsButton == null)
            applyImprovementsButton = FindButtonWithLabel(root, "Apply Improvements");

        if (recalibrateButton == null)
            recalibrateButton = FindButtonWithLabel(root, "Recalibrate Movement");

        // Performance bars: three Scrollbars named (top to bottom): Scrollbar, Scrollbar (1), Scrollbar (2)
        // They align with the three challenge score rows.
        // Row order (top -> bottom): Scrollbar, Scrollbar (1), Scrollbar (2)
        if (driftBar == null) driftBar = FindScrollbarByName(root, "Scrollbar");
        if (cameraBar == null) cameraBar = FindScrollbarByName(root, "Scrollbar (1)");
        if (speedBar == null) speedBar = FindScrollbarByName(root, "Scrollbar (2)");
    }

    private void RebindButtons()
    {
        if (applyImprovementsButton != null)
        {
            applyImprovementsButton.onClick.RemoveListener(OnApplyImprovementsClicked);
            applyImprovementsButton.onClick.AddListener(OnApplyImprovementsClicked);
        }

        if (recalibrateButton != null)
        {
            recalibrateButton.onClick.RemoveListener(OnRecalibrateClicked);
            recalibrateButton.onClick.AddListener(OnRecalibrateClicked);
        }
    }

    private static TMP_Text FindTMPByName(Transform root, string goName)
    {
        if (root == null) return null;

        Transform t = root.Find(goName);
        if (t != null) return t.GetComponent<TMP_Text>();

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.name == goName) return all[i];
        }

        return null;
    }

    private static TMP_Text FindTMPContains(Transform root, string textFragment)
    {
        if (string.IsNullOrWhiteSpace(textFragment) || root == null) return null;

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp != null && !string.IsNullOrEmpty(tmp.text) && tmp.text.Contains(textFragment))
                return tmp;
        }

        return null;
    }

    private static Button FindButtonWithLabel(Transform root, string labelText)
    {
        if (root == null) return null;

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp == null || tmp.text != labelText) continue;

            Button b = tmp.GetComponentInParent<Button>();
            if (b != null) return b;
        }

        return null;
    }

    private static Scrollbar FindScrollbarByName(Transform root, string goName)
    {
        if (root == null) return null;

        Transform t = root.Find(goName);
        if (t != null) return t.GetComponent<Scrollbar>();

        Scrollbar[] all = root.GetComponentsInChildren<Scrollbar>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.name == goName) return all[i];
        }

        return null;
    }
}
