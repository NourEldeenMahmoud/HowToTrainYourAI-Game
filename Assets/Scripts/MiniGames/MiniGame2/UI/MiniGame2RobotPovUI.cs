using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MiniGame2RobotPovUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame2Manager miniGame2Manager;
    [Tooltip("Root of the Robot POV UI (e.g. Robot POV Canvas / pod UI root). If null, will use this GameObject.")]
    [SerializeField] private Transform povUiRoot;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text challengeNameText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text movesNumberText;
    [SerializeField] private TMP_Text robotStatusText;
    [SerializeField] private TMP_Text hintText;

    [Header("UI Bars")]
    [SerializeField] private Image energyFillImage;

    [Header("MG2 Text")]
    [SerializeField] private string instructionMessage = "Find the audio card in order to read the message properly.";
    [SerializeField] private string readyStatusText = "READY";
    [SerializeField] private string movingStatusText = "MOVING";
    [SerializeField] private string doneStatusText = "DONE";
    [SerializeField] private string failedStatusText = "FAILED";

    [Header("Behavior")]
    [SerializeField] private bool autoFind = true;
    [SerializeField] private bool autoCreatePrefabInEditor = true;
    [SerializeField] private string editorPrefabPath = "Assets/prefabs/UI Prefabs/MG2 Robot pov Prefabs/Game 2 Robot POV Canvas.prefab";
    [SerializeField] private string runtimeCanvasName = "Game 2 Robot POV Canvas";
    [SerializeField] private bool showOnSceneStart = true;
    [SerializeField] private bool hideOnMiniGameCompleted = true;
    [SerializeField, Min(1)] private int maxLogLines = 6;
    [SerializeField, Min(5)] private int maxCharsPerLine = 28;
    [SerializeField] private bool autoFindTabButton = true;
    [SerializeField] private Button tabSwitchButton;
    [SerializeField, Range(0f, 1f)] private float disabledTabAlpha = 0.35f;

    private MiniGame2Phase lastPhase = MiniGame2Phase.Idle;
    private readonly List<string> logLines = new List<string>();
    private Graphic tabSwitchButtonGraphic;
    private float tabSwitchOriginalAlpha = 1f;
    private bool hasCompleted;
    private RectTransform energyFillRect;
    private float energyFillOriginalAnchorMaxX = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureUiControllerAfterSceneLoad()
    {
        if (FindFirstObjectByType<MiniGame2Manager>() == null)
            return;

        if (FindFirstObjectByType<MiniGame2RobotPovUI>() != null)
            return;

        GameObject go = new GameObject("MG2_RobotPovUI_Controller");
        go.AddComponent<MiniGame2RobotPovUI>();
    }

    private void OnEnable()
    {
        ResolvePovRoot();

        if (autoFind)
        {
            AutoFindUI();
        }

        if (miniGame2Manager == null)
        {
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
        }

        if (autoFindTabButton)
            AutoFindTabSwitchButton();

        CacheTabButtonGraphic();
        CacheEnergyFillRect();

        if (miniGame2Manager != null)
        {
            miniGame2Manager.PhaseChanged += OnPhaseChanged;
            miniGame2Manager.LogMessage += AppendLog;
            miniGame2Manager.MiniGameCompleted += OnMiniGameCompleted;
            OnPhaseChanged(miniGame2Manager.CurrentPhase);

            bool disableTab = miniGame2Manager.CurrentPhase == MiniGame2Phase.Planning || miniGame2Manager.CurrentPhase == MiniGame2Phase.RobotMoving;
            SetTabButtonVisualEnabled(!disableTab);
        }

        if (showOnSceneStart && povUiRoot != null)
            povUiRoot.gameObject.SetActive(true);

        ApplyStaticText();
        RefreshDynamicUI();
    }

    private void OnDisable()
    {
        SetTabButtonVisualEnabled(true);

        if (miniGame2Manager != null)
        {
            miniGame2Manager.PhaseChanged -= OnPhaseChanged;
            miniGame2Manager.LogMessage -= AppendLog;
            miniGame2Manager.MiniGameCompleted -= OnMiniGameCompleted;
        }
    }

    private void Update()
    {
        if (clockText != null)
        {
            clockText.text = DateTime.Now.ToString("HH:mm:ss");
        }

        ApplyStaticText();
        RefreshDynamicUI();
    }

    private void OnPhaseChanged(MiniGame2Phase phase)
    {
        if (phase == lastPhase) return;
        lastPhase = phase;

        switch (phase)
        {
            case MiniGame2Phase.Idle:
                SetChallengeName(instructionMessage);
                SetPrompt("");
                break;
            case MiniGame2Phase.Planning:
                SetChallengeName(instructionMessage);
                SetPrompt("");
                SetLog("");
                break;
            case MiniGame2Phase.RobotMoving:
                SetChallengeName(instructionMessage);
                SetPrompt("");
                break;
            case MiniGame2Phase.Completed:
                SetChallengeName(instructionMessage);
                SetPrompt("");
                break;
            default:
                break;
        }

        bool disableTab = phase == MiniGame2Phase.Planning || phase == MiniGame2Phase.RobotMoving;
        SetTabButtonVisualEnabled(!disableTab);
        RefreshDynamicUI();
    }

    private void OnMiniGameCompleted(MiniGame2EvaluationResult result)
    {
        hasCompleted = true;
        RefreshDynamicUI();

        if (hideOnMiniGameCompleted && povUiRoot != null)
            povUiRoot.gameObject.SetActive(false);
    }

    private void SetChallengeName(string name)
    {
        if (challengeNameText != null) challengeNameText.text = name;
        if (instructionText != null) instructionText.text = instructionMessage;
    }

    private void SetPrompt(string msg)
    {
        if (promptText != null) promptText.text = msg;
    }

    private void SetLog(string msg)
    {
        logLines.Clear();
        if (!string.IsNullOrEmpty(msg))
            logLines.Add(msg);
        FlushLog();
    }

    private void AppendLog(string msg)
    {
        if (logText == null) return;
        if (string.IsNullOrEmpty(msg)) return;

        if (msg.Length > maxCharsPerLine)
            msg = msg.Substring(0, maxCharsPerLine - 1) + "\u2026";

        logLines.Add(msg);
        while (logLines.Count > maxLogLines)
            logLines.RemoveAt(0);

        FlushLog();
    }

    private void FlushLog()
    {
        if (logText == null) return;
        logText.text = string.Join("\n", logLines);
    }

    private void AutoFindUI()
    {
        ResolvePovRoot();
        Transform root = povUiRoot != null ? povUiRoot : transform;
        SearchHierarchyForUI(root);
    }

    private bool AnyUIFieldMissing()
    {
        return challengeNameText == null || promptText == null || logText == null || clockText == null ||
               instructionText == null || movesNumberText == null || robotStatusText == null || energyFillImage == null;
    }

    private void SearchHierarchyForUI(Transform root)
    {
        if (challengeNameText == null)
        {
            challengeNameText = FindTMPByName(root, "Instruction Text");

            Transform objective = FindChildByName(root, "Objective Texts");
            if (objective != null)
                challengeNameText = FindTMPByName(objective, "Instruction Text");

            if (challengeNameText == null)
                challengeNameText = FindTMPByName(root, "Instruction Text (Title) ");
        }

        if (instructionText == null)
            instructionText = FindTMPByName(root, "Instruction Text");

        if (movesNumberText == null)
            movesNumberText = FindTMPByName(root, "Moves Number Text");

        if (robotStatusText == null)
            robotStatusText = FindTMPByName(root, "Robot Status Text");

        if (hintText == null)
            hintText = FindTMPByName(root, "Hint Text");

        if (energyFillImage == null)
        {
            Transform energy = FindChildByName(root, "Energy Bar");
            if (energy != null)
            {
                Transform fill = FindChildByName(energy, "Fill");
                if (fill != null)
                    energyFillImage = fill.GetComponent<Image>();
            }
        }

        CacheEnergyFillRect();

        if (promptText == null)
        {
            Transform err = FindChildByName(root, "Error Message");
            if (err != null)
                promptText = FindTMPByName(err, "Text (TMP)");
        }

        if (logText == null)
        {
            Transform status = FindChildByName(root, "Status Messages");
            if (status != null)
                logText = FindTMPByName(status, "Text (TMP)");
        }

        if (clockText == null)
        {
            Transform timer = FindChildByName(root, "TimerText");
            if (timer != null)
                clockText = FindTMPByName(timer, "Text (TMP)");
        }
    }

    private TMP_Text FindTMPByName(Transform root, string goName)
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

    private Transform FindChildByName(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    private void ResolvePovRoot()
    {
        if (povUiRoot != null && povUiRoot.childCount > 0)
            return;

        GameObject existing = FindGameObjectByNameIncludingInactive(runtimeCanvasName);
        if (existing != null)
        {
            povUiRoot = existing.transform;
            return;
        }

#if UNITY_EDITOR
        if (!autoCreatePrefabInEditor)
        {
            if (povUiRoot == null) povUiRoot = transform;
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorPrefabPath);
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            instance.name = runtimeCanvasName;
            povUiRoot = instance.transform;
            return;
        }
#endif

        if (povUiRoot == null)
            povUiRoot = transform;
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

    private void ApplyStaticText()
    {
        if (instructionText != null)
            instructionText.text = instructionMessage;

        if (challengeNameText != null)
            challengeNameText.text = instructionMessage;
    }

    private void RefreshDynamicUI()
    {
        if (miniGame2Manager == null)
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();

        if (miniGame2Manager == null)
            return;

        if (movesNumberText != null)
            movesNumberText.text = miniGame2Manager.ActualMoveCount.ToString();

        if (energyFillImage != null)
        {
            float maxEnergy = Mathf.Max(0.01f, miniGame2Manager.StartingEnergyBudget);
            float t = Mathf.Clamp01(miniGame2Manager.RemainingEnergy / maxEnergy);
            energyFillImage.fillAmount = t;

            CacheEnergyFillRect();
            if (energyFillRect != null)
            {
                Vector2 anchorMax = energyFillRect.anchorMax;
                anchorMax.x = energyFillOriginalAnchorMaxX * t;
                energyFillRect.anchorMax = anchorMax;
            }
        }

        if (robotStatusText != null)
            robotStatusText.text = GetStatusText();
    }

    private string GetStatusText()
    {
        if (miniGame2Manager == null)
            return readyStatusText;

        if (hasCompleted || miniGame2Manager.CurrentPhase == MiniGame2Phase.Completed)
            return miniGame2Manager.HasPassedLastRun ? doneStatusText : failedStatusText;

        if (miniGame2Manager.CurrentPhase == MiniGame2Phase.RobotMoving)
            return movingStatusText;

        return readyStatusText;
    }

    private void CacheEnergyFillRect()
    {
        if (energyFillImage == null)
            return;

        if (energyFillRect != null)
            return;

        energyFillRect = energyFillImage.rectTransform;
        if (energyFillRect != null)
            energyFillOriginalAnchorMaxX = Mathf.Max(0.001f, energyFillRect.anchorMax.x);
    }

    private void AutoFindTabSwitchButton()
    {
        if (tabSwitchButton != null)
            return;

        Transform root = povUiRoot != null ? povUiRoot : transform;
        if (root == null)
            return;

        Button[] allButtons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button == null)
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                continue;

            string text = label.text != null ? label.text.Trim() : string.Empty;
            if (text.Equals("tab", StringComparison.OrdinalIgnoreCase) || text.IndexOf("tab", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tabSwitchButton = button;
                break;
            }
        }
    }

    private void CacheTabButtonGraphic()
    {
        if (tabSwitchButton == null)
            return;

        if (tabSwitchButtonGraphic != null)
            return;

        tabSwitchButtonGraphic = tabSwitchButton.targetGraphic;
        if (tabSwitchButtonGraphic == null)
            return;

        tabSwitchOriginalAlpha = tabSwitchButtonGraphic.color.a;
    }

    private void SetTabButtonVisualEnabled(bool enabled)
    {
        if (tabSwitchButton == null)
            return;

        tabSwitchButton.gameObject.SetActive(enabled);
        tabSwitchButton.interactable = enabled;

        CacheTabButtonGraphic();
        if (tabSwitchButtonGraphic == null)
            return;

        Color color = tabSwitchButtonGraphic.color;
        color.a = enabled ? tabSwitchOriginalAlpha : Mathf.Min(tabSwitchOriginalAlpha, disabledTabAlpha);
        tabSwitchButtonGraphic.color = color;
    }
}
