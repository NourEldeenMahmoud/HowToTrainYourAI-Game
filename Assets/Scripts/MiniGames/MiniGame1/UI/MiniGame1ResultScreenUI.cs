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
            miniGame1Manager.MiniGameCompleted += OnMiniGameCompleted;

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
        if (resultScreenRoot != null)
            resultScreenRoot.SetActive(true);

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
        ApplyBar(speedBar, r.challengeScores.speedScore);
        ApplyBar(cameraBar, r.challengeScores.cameraScore);
        ApplyBar(driftBar, r.challengeScores.driftScore);
    }

    private static void ApplyBar(Scrollbar bar, float score0To100)
    {
        if (bar == null) return;
        float t = Mathf.Clamp01(score0To100 / 100f);
        // Don't set interactable=false because it forces DisabledColor (greys out).
        // Instead, keep visuals as-is and just prevent navigation.
        bar.interactable = true;
        var nav = bar.navigation;
        nav.mode = Navigation.Mode.None;
        bar.navigation = nav;
        // Treat handle as progress fill from LEFT to RIGHT.
        bar.direction = Scrollbar.Direction.LeftToRight;
        bar.size = t;
        bar.value = 0f;
    }

    private void OnApplyImprovementsClicked()
    {
        if (resultScreenRoot != null) resultScreenRoot.SetActive(false);

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(false);
    }

    private void OnRecalibrateClicked()
    {
        if (miniGame1Manager != null)
            miniGame1Manager.StartMiniGame();

        if (resultScreenRoot != null) resultScreenRoot.SetActive(false);

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(false);
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
        if (speedBar == null) speedBar = FindScrollbarByName(root, "Scrollbar");
        if (cameraBar == null) cameraBar = FindScrollbarByName(root, "Scrollbar (1)");
        if (driftBar == null) driftBar = FindScrollbarByName(root, "Scrollbar (2)");
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
