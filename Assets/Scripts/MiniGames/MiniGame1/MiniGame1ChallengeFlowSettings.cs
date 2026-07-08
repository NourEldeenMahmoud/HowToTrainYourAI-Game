using System;
using UnityEngine;

[Serializable]
public struct MiniGame1PhaseUiText
{
    [Tooltip("Mini Game 1 phase that this UI text belongs to.")]
    public MiniGame1Manager.MiniGame1Phase phase;

    [Tooltip("Short challenge label shown in Robot POV when this phase is active. Suggested: 1-3 words.")]
    public string challengeText;

    [Tooltip("Short red prompt/error hint shown in Robot POV. Leave empty for normal free-move phases.")]
    public string promptText;
}

[Serializable]
public class MiniGame1ChallengeFlowSettings
{
    [Header("Phase Text")]
    [Tooltip("Fallback challenge label if a phase has no explicit text entry. Suggested: empty.")]
    public string fallbackChallengeText = "";

    [Tooltip("Fallback prompt if a phase has no explicit text entry. Suggested: empty.")]
    public string fallbackPromptText = "";

    [Tooltip("Single source of truth for Robot POV challenge labels/prompts during Mini Game 1.")]
    public MiniGame1PhaseUiText[] phaseTexts = CreateDefaultPhaseTexts();

    public MiniGame1PhaseUiText GetText(MiniGame1Manager.MiniGame1Phase phase)
    {
        if (phaseTexts != null)
        {
            for (int i = 0; i < phaseTexts.Length; i++)
            {
                if (phaseTexts[i].phase == phase)
                    return phaseTexts[i];
            }
        }

        return new MiniGame1PhaseUiText
        {
            phase = phase,
            challengeText = fallbackChallengeText,
            promptText = fallbackPromptText
        };
    }

    public static MiniGame1ChallengeFlowSettings CreateDefault()
    {
        return new MiniGame1ChallengeFlowSettings
        {
            fallbackChallengeText = "",
            fallbackPromptText = "",
            phaseTexts = CreateDefaultPhaseTexts()
        };
    }

    public static MiniGame1PhaseUiText[] CreateDefaultPhaseTexts()
    {
        return new[]
        {
            Text(MiniGame1Manager.MiniGame1Phase.None, "", ""),
            Text(MiniGame1Manager.MiniGame1Phase.FreeMoveInitial, "Free Move", ""),
            Text(MiniGame1Manager.MiniGame1Phase.DriftLeft, "Drift (1)", "Go Right / Go Left"),
            Text(MiniGame1Manager.MiniGame1Phase.FreeMoveBetween_DriftLeft_DriftRight, "Free Move", ""),
            Text(MiniGame1Manager.MiniGame1Phase.DriftRight, "Drift (2)", "Go Right / Go Left"),
            Text(MiniGame1Manager.MiniGame1Phase.FreeMoveBetween_DriftRight_Camera, "Free Move", ""),
            Text(MiniGame1Manager.MiniGame1Phase.CameraAlignment, "Camera", "Fix camera"),
            Text(MiniGame1Manager.MiniGame1Phase.FreeMoveBetween_Camera_Speed, "Free Move", ""),
            Text(MiniGame1Manager.MiniGame1Phase.SpeedConsistency, "Speed", "Hold speed"),
            Text(MiniGame1Manager.MiniGame1Phase.Completed, "Done", "")
        };
    }

    private static MiniGame1PhaseUiText Text(MiniGame1Manager.MiniGame1Phase phase, string challenge, string prompt)
    {
        return new MiniGame1PhaseUiText
        {
            phase = phase,
            challengeText = challenge,
            promptText = prompt
        };
    }
}
