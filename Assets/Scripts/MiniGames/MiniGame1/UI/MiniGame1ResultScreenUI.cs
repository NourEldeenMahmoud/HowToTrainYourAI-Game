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
}
