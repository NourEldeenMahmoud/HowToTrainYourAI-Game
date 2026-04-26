using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MiniGame2ResultScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame2Manager miniGame2Manager;
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private GameObject resultScreenRoot;

    [Header("Discovery")]
    [SerializeField] private bool autoFindOnEnable = true;
    [SerializeField] private string resultCanvasName = "Game2 Result Screen Canvas";

    [Header("UI - Texts")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text energyEfficiencyPercentText;
    [SerializeField] private TMP_Text routeQualityPercentText;
    [SerializeField] private TMP_Text movesTakenText;
    [SerializeField] private TMP_Text missionOutcomeText;
    [SerializeField] private TMP_Text missionDetailsText;
    [SerializeField] private TMP_Text usedEnergyValueText;
    [SerializeField] private TMP_Text totalEnergyBudgetText;
    [SerializeField] private TMP_Text tierText;

    [Header("UI - Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;

    [Header("UI - Bars (Sliders)")]
    [SerializeField] private Slider energyEfficiencyBar;
    [SerializeField] private Slider pathEfficiencyBar;
    [SerializeField] private Slider finalScoreBar;

    [Header("Outcome Copy")]
    [SerializeField] private string winTitle = "MISSION COMPLETE!";
    [TextArea]
    [SerializeField] private string winDetails = "GOOD JOB!\n\nYou saved energy and reached the goal successfully.";
    [SerializeField] private string loseTitle = "MISSION FAILED!";
    [TextArea]
    [SerializeField] private string loseDetails = "MISSION FAILED!\n\nEnergy ran out before you reached the audio card.";

    [Header("Behavior")]
    [SerializeField] private bool lockControlsWhileVisible = true;
    [SerializeField] private bool hideScreenOnEnable = true;
    [SerializeField] private bool disableNextButton = true;
    [SerializeField] private bool debugToConsole = true;

    private MiniGame2EvaluationResult lastResult;
    private bool hasResult;
    private bool nextSequenceStarted;

    private void OnEnable()
    {
        hasResult = false;
        nextSequenceStarted = false;

        ResolveCoreReferences();

        if (autoFindOnEnable)
            AutoFind();

        RebindButtons();
        ConfigureNextButtonState();

        if (miniGame2Manager != null)
        {
            miniGame2Manager.MiniGameCompleted -= OnMiniGameCompleted;
            miniGame2Manager.MiniGameCompleted += OnMiniGameCompleted;

            if (miniGame2Manager.CurrentPhase == MiniGame2Phase.Completed)
            {
                OnMiniGameCompleted(miniGame2Manager.LastResult);
                return;
            }
        }

        if (hideScreenOnEnable)
            SetResultScreenVisible(false);
    }

    private void OnDisable()
    {
        if (miniGame2Manager != null)
            miniGame2Manager.MiniGameCompleted -= OnMiniGameCompleted;

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void ResolveCoreReferences()
    {
        if (miniGame2Manager == null)
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        if (resultScreenRoot != null)
            return;

        resultScreenRoot = FindGameObjectByNameIncludingInactive(resultCanvasName);
        if (resultScreenRoot != null)
            return;

        resultScreenRoot = FindGameObjectByNameFragmentIncludingInactive("Result Screen Canvas");
        if (resultScreenRoot != null)
            return;

        Canvas canvasInChildren = GetComponentInChildren<Canvas>(true);
        if (canvasInChildren != null)
            resultScreenRoot = canvasInChildren.gameObject;
    }

    private void AutoFind()
    {
        Transform root = resultScreenRoot != null ? resultScreenRoot.transform : null;
        if (root == null)
            return;

        if (finalScoreText == null)
            finalScoreText = FindTMPByName(root, "Final Result Number");

        if (energyEfficiencyPercentText == null)
            energyEfficiencyPercentText = FindTMPByName(root, "Energy Efficiency Number");

        if (routeQualityPercentText == null)
            routeQualityPercentText = FindTMPByName(root, "Route Quality Number");

        if (movesTakenText == null)
            movesTakenText = FindTMPByName(root, "Moves Taken Number");

        if (missionOutcomeText == null)
            missionOutcomeText = FindTMPByName(root, "Mission Complete Text");

        if (missionDetailsText == null)
            missionDetailsText = FindTMPContains(root, "You saved energy");

        Transform energySection = FindChildByName(root, "Energy Efficiency");
        Transform scopedRoot = energySection != null ? energySection : root;

        if (usedEnergyValueText == null)
            usedEnergyValueText = FindTMPByName(scopedRoot, "Text 2");

        if (totalEnergyBudgetText == null)
            totalEnergyBudgetText = FindTMPByName(scopedRoot, "Text 4") ?? FindTMPContains(scopedRoot, "energy.");

        if (retryButton == null)
            retryButton = FindButtonByName(root, "Retry Button") ?? FindButtonWithLabel(root, "Retry");

        if (nextButton == null)
            nextButton = FindButtonByName(root, "Next Button") ?? FindButtonWithLabel(root, "Next");

        if (energyEfficiencyBar == null)
            energyEfficiencyBar = FindSliderByName(root, "Energy Slider");

        if (pathEfficiencyBar == null)
            pathEfficiencyBar = FindSliderByName(root, "Route Slider");

        if (finalScoreBar == null)
            finalScoreBar = FindSliderByName(root, "Final Result Slider");
    }

    private void RebindButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    private void OnMiniGameCompleted(MiniGame2EvaluationResult result)
    {
        lastResult = result;
        hasResult = true;
        nextSequenceStarted = false;

        if (resultScreenRoot == null)
        {
            ResolveCoreReferences();
            if (autoFindOnEnable)
                AutoFind();
        }

        EnsureEventSystemExists();

        SetResultScreenVisible(true);

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(true);

        ApplyResult(result);
        ConfigureNextButtonState();

        if (debugToConsole)
            Debug.Log($"[MG2][ResultUI] Result shown. final={result.finalScore:F1} tier={result.tier} success={result.isSuccess} energyFail={result.failedByEnergyDepletion}", this);
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private void ApplyResult(MiniGame2EvaluationResult result)
    {
        if (finalScoreText != null)
            finalScoreText.text = $"{FormatPercent(result.finalScore, result.isSuccess)}%";

        if (energyEfficiencyPercentText != null)
            energyEfficiencyPercentText.text = $"{FormatPercent(result.energyEfficiencyScore, result.isSuccess)}%";

        if (routeQualityPercentText != null)
            routeQualityPercentText.text = $"{FormatPercent(result.pathEfficiencyScore, result.isSuccess)}%";

        if (movesTakenText != null)
            movesTakenText.text = result.actualStepCount.ToString();

        if (tierText != null)
            tierText.text = result.tier.ToString();

        bool isLoseOutcome = !result.isSuccess;

        if (missionOutcomeText != null)
            missionOutcomeText.text = isLoseOutcome ? loseTitle : winTitle;

        if (missionDetailsText != null)
            missionDetailsText.text = isLoseOutcome ? loseDetails : winDetails;

        if (usedEnergyValueText != null)
            usedEnergyValueText.text = Mathf.RoundToInt(result.actualEnergy).ToString();

        if (totalEnergyBudgetText != null)
        {
            float configuredBudget = miniGame2Manager != null ? miniGame2Manager.StartingEnergyBudget : 0f;
            float displayBudget = configuredBudget > 0f ? configuredBudget : Mathf.Max(1f, result.actualEnergy);
            totalEnergyBudgetText.text = $"{Mathf.RoundToInt(displayBudget)} energy.";
        }

        ApplyBar(energyEfficiencyBar, result.energyEfficiencyScore);
        ApplyBar(pathEfficiencyBar, result.pathEfficiencyScore);
        ApplyBar(finalScoreBar, result.finalScore);
    }

    private static int FormatPercent(float score, bool isSuccess)
    {
        return isSuccess
            ? Mathf.RoundToInt(score)
            : Mathf.FloorToInt(score);
    }

    private void ConfigureNextButtonState()
    {
        if (nextButton == null)
            return;

        bool hasSuccessfulResult = ResolveSuccessfulResultState();
        bool enabled = hasSuccessfulResult && !nextSequenceStarted;

        if (!hasResult && disableNextButton)
            enabled = false;

        nextButton.interactable = enabled;

        Navigation nav = nextButton.navigation;
        nav.mode = enabled ? Navigation.Mode.Automatic : Navigation.Mode.None;
        nextButton.navigation = nav;
    }

    private bool ResolveSuccessfulResultState()
    {
        if (hasResult)
            return lastResult.isSuccess;

        if (miniGame2Manager == null)
            return false;

        if (miniGame2Manager.CurrentPhase != MiniGame2Phase.Completed)
            return false;

        MiniGame2EvaluationResult managerResult = miniGame2Manager.LastResult;
        return managerResult.isSuccess || miniGame2Manager.HasPassedLastRun;
    }

    private static void ApplyBar(Slider bar, float score0To100)
    {
        if (bar == null)
            return;

        float t = Mathf.Clamp01(score0To100 / 100f);

        bar.transition = Selectable.Transition.None;
        bar.interactable = false;
        Navigation nav = bar.navigation;
        nav.mode = Navigation.Mode.None;
        bar.navigation = nav;

        if (bar.fillRect != null)
        {
            Graphic fillGraphic = bar.fillRect.GetComponent<Graphic>();
            if (fillGraphic != null)
                fillGraphic.raycastTarget = false;
        }

        if (bar.targetGraphic != null)
            bar.targetGraphic.raycastTarget = false;

        bar.direction = Slider.Direction.LeftToRight;
        bar.normalizedValue = t;
    }

    private void OnRetryClicked()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void OnNextClicked()
    {
        if (!hasResult || !lastResult.isSuccess)
            return;

        if (nextSequenceStarted)
            return;

        nextSequenceStarted = true;

        if (retryButton != null)
            retryButton.interactable = false;

        HideResultScreenForNextSequence();
        ConfigureNextButtonState();

        if (miniGame2Manager == null)
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();

        if (miniGame2Manager == null)
        {
            if (debugToConsole)
                Debug.LogWarning("[MG2][ResultUI] Next clicked but MiniGame2Manager was not found.", this);

            nextSequenceStarted = false;
            if (retryButton != null)
                retryButton.interactable = true;
            ConfigureNextButtonState();
            return;
        }

        if (debugToConsole)
            Debug.Log("[MG2][ResultUI] Next clicked. Starting return-to-gate sequence.", this);

        miniGame2Manager.StartReturnToGateAndExitSequence();
    }

    private void HideResultScreenForNextSequence()
    {
        if (resultScreenRoot == null)
            return;

        bool rootWasSelf = resultScreenRoot == gameObject;
        SetResultScreenVisible(false);

        if (!rootWasSelf)
            return;

        Transform root = resultScreenRoot.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }
    }

    private void SetResultScreenVisible(bool visible)
    {
        if (resultScreenRoot == null)
            return;

        if (!visible && resultScreenRoot == gameObject)
        {
            if (debugToConsole)
                Debug.LogWarning("[MG2][ResultUI] resultScreenRoot points to this controller object; skipping hide to avoid disabling this script.", this);
            return;
        }

        resultScreenRoot.SetActive(visible);

        if (!visible)
            return;

        Transform rootTransform = resultScreenRoot.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform child = rootTransform.GetChild(i);
            if (child != null && !child.gameObject.activeSelf)
                child.gameObject.SetActive(true);
        }
    }

    private static GameObject FindGameObjectByNameIncludingInactive(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        GameObject active = GameObject.Find(objectName);
        if (active != null)
            return active;

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t != null && t.gameObject.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return t.gameObject;
        }

        return null;
    }

    private static GameObject FindGameObjectByNameFragmentIncludingInactive(string objectNameFragment)
    {
        if (string.IsNullOrEmpty(objectNameFragment))
            return null;

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t == null)
                continue;

            if (t.gameObject.name.IndexOf(objectNameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                return t.gameObject;
        }

        return null;
    }

    private static TMP_Text FindTMPByName(Transform root, string goName)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(goName);
        if (direct != null)
            return direct.GetComponent<TMP_Text>();

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp != null && tmp.gameObject.name == goName)
                return tmp;
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static TMP_Text FindTMPContains(Transform root, string textFragment)
    {
        if (root == null || string.IsNullOrWhiteSpace(textFragment))
            return null;

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp != null && !string.IsNullOrEmpty(tmp.text) && tmp.text.IndexOf(textFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                return tmp;
        }

        return null;
    }

    private static Button FindButtonByName(Transform root, string goName)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(goName);
        if (direct != null)
            return direct.GetComponent<Button>();

        Button[] all = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Button button = all[i];
            if (button != null && button.gameObject.name == goName)
                return button;
        }

        return null;
    }

    private static Button FindButtonWithLabel(Transform root, string labelText)
    {
        if (root == null)
            return null;

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp == null)
                continue;

            if (!string.Equals(tmp.text, labelText, StringComparison.OrdinalIgnoreCase))
                continue;

            Button button = tmp.GetComponentInParent<Button>();
            if (button != null)
                return button;
        }

        return null;
    }

    private static Slider FindSliderByName(Transform root, string goName)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(goName);
        if (direct != null)
            return direct.GetComponent<Slider>();

        Slider[] all = root.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Slider bar = all[i];
            if (bar != null && bar.gameObject.name == goName)
                return bar;
        }

        return null;
    }
}
