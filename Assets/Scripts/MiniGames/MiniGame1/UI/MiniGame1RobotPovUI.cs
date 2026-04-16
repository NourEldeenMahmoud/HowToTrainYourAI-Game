using System;
using TMPro;
using UnityEngine;

public class MiniGame1RobotPovUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame1Manager miniGame1Manager;
    [Tooltip("Root of the Robot POV UI (e.g. Robot POV Canvas / pod UI root). If null, will use this GameObject.")]
    [SerializeField] private Transform povUiRoot;

    [Header("UI Texts")]
    [Tooltip("Challenge name text. In Robot POV this is usually Objective Texts -> Instruction Text (Reach The Target).")]
    [SerializeField] private TMP_Text challengeNameText;
    [Tooltip("Right-side red text. In Robot POV this is Error Message -> Text (TMP).")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private TMP_Text clockText;

    [Header("Behavior")]
    [SerializeField] private bool autoFind = true;

    private MiniGame1Manager.MiniGame1Phase lastPhase = MiniGame1Manager.MiniGame1Phase.None;

    private void OnEnable()
    {
        if (povUiRoot == null) povUiRoot = transform;

        if (autoFind)
        {
            AutoFindUI();
        }

        if (miniGame1Manager == null)
        {
            miniGame1Manager = FindFirstObjectByType<MiniGame1Manager>();
        }

        if (miniGame1Manager != null)
        {
            miniGame1Manager.PhaseChanged += OnPhaseChanged;
            OnPhaseChanged(miniGame1Manager.CurrentPhase);
        }
    }

    private void OnDisable()
    {
        if (miniGame1Manager != null)
        {
            miniGame1Manager.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void Update()
    {
        if (clockText != null)
        {
            clockText.text = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    private void OnPhaseChanged(MiniGame1Manager.MiniGame1Phase phase)
    {
        if (phase == lastPhase) return;
        lastPhase = phase;

        switch (phase)
        {
            case MiniGame1Manager.MiniGame1Phase.FreeMoveInitial:
                SetChallengeName("Free Move");
                SetPrompt("");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.DriftLeft:
                SetChallengeName("Drift (1)");
                SetPrompt("Go Right / Go Left");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.FreeMoveBetween_DriftLeft_DriftRight:
                SetChallengeName("Free Move");
                SetPrompt("");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.DriftRight:
                SetChallengeName("Drift (2)");
                SetPrompt("Go Right / Go Left");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.FreeMoveBetween_DriftRight_Camera:
                SetChallengeName("Free Move");
                SetPrompt("");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.CameraAlignment:
                SetChallengeName("Camera");
                SetPrompt("Fix camera");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.FreeMoveBetween_Camera_Speed:
                SetChallengeName("Free Move");
                SetPrompt("");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.SpeedConsistency:
                SetChallengeName("Speed");
                SetPrompt("Hold speed");
                SetLog(" ");
                break;

            case MiniGame1Manager.MiniGame1Phase.Completed:
                SetChallengeName("Done");
                SetPrompt("");
                SetLog(" ");
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
        if (logText != null) logText.text = msg;
    }

    private void AutoFindUI()
    {
        Transform root = povUiRoot != null ? povUiRoot : transform;

        if (challengeNameText == null)
        {
            // Prefer the visible "Reach The Target" text (Objective Texts -> Instruction Text).
            Transform objective = FindChildByName(root, "Objective Texts");
            if (objective != null)
                challengeNameText = FindTMPByName(objective, "Instruction Text");

            // Fallback to "Instruction Text (Title) " if Objective isn't present.
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
            // Robot POV prefab uses: Status Messages -> Text (TMP)
            Transform status = FindChildByName(root, "Status Messages");
            if (status != null)
                logText = FindTMPByName(status, "Text (TMP)");
        }

        if (clockText == null)
        {
            // Robot POV prefab uses: TimerText -> Text (TMP)
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

