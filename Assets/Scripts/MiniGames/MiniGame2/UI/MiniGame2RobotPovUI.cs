using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [Header("Behavior")]
    [SerializeField] private bool autoFind = true;
    [SerializeField, Min(1)] private int maxLogLines = 6;
    [SerializeField, Min(5)] private int maxCharsPerLine = 28;

    private MiniGame2Phase lastPhase = MiniGame2Phase.Idle;
    private readonly List<string> logLines = new List<string>();

    private void OnEnable()
    {
        if (povUiRoot == null) povUiRoot = transform;

        if (autoFind)
        {
            AutoFindUI();
        }

        if (miniGame2Manager == null)
        {
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
        }

        if (miniGame2Manager != null)
        {
            miniGame2Manager.PhaseChanged += OnPhaseChanged;
            miniGame2Manager.LogMessage += AppendLog;
            OnPhaseChanged(miniGame2Manager.CurrentPhase);
        }
    }

    private void OnDisable()
    {
        if (miniGame2Manager != null)
        {
            miniGame2Manager.PhaseChanged -= OnPhaseChanged;
            miniGame2Manager.LogMessage -= AppendLog;
        }
    }

    private void Update()
    {
        if (clockText != null)
        {
            clockText.text = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    private void OnPhaseChanged(MiniGame2Phase phase)
    {
        if (phase == lastPhase) return;
        lastPhase = phase;

        switch (phase)
        {
            case MiniGame2Phase.Idle:
                SetChallengeName("");
                SetPrompt("");
                break;
            case MiniGame2Phase.Planning:
                SetChallengeName("Plan Route");
                SetPrompt("Click tiles");
                SetLog("");
                break;
            case MiniGame2Phase.RobotMoving:
                SetChallengeName("Executing");
                SetPrompt("");
                break;
            case MiniGame2Phase.Completed:
                SetChallengeName("Done");
                SetPrompt("");
                break;
            default:
                break;
        }
    }

    private void SetChallengeName(string name)
    {
        if (challengeNameText != null) challengeNameText.text = name;
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
        Transform root = povUiRoot != null ? povUiRoot : transform;

        if (challengeNameText == null)
        {
            Transform objective = FindChildByName(root, "Objective Texts");
            if (objective != null)
                challengeNameText = FindTMPByName(objective, "Instruction Text");

            if (challengeNameText == null)
                challengeNameText = FindTMPByName(root, "Instruction Text (Title) ");
        }

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
}

