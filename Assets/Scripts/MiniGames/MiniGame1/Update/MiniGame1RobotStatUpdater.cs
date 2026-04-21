using UnityEngine;

public static class MiniGame1RobotStatUpdater
{
    public static void ApplyUpdateOnce(
        MiniGame1LearningProfileSO profile,
        RobotStatsSO robotStats,
        MiniGame1EvaluationResult eval)
    {
        if (profile == null || robotStats == null) return;

        if (eval.tier == MiniGameTier.Fail)
        {
            // Fail => no update (per design).
            return;
        }

        MiniGame1TierDeltas deltas = profile.GetTierDeltas(eval.tier);
        robotStats.ApplyDelta(deltas.stabilityDelta, deltas.pathAccuracyDelta, deltas.inputResponsivenessDelta);

        float driftRate = profile.ScoreToErrorRate(MiniGame1ChallengeType.Drift, eval.challengeScores.driftScore);
        float camRate = profile.ScoreToErrorRate(MiniGame1ChallengeType.CameraAlignment, eval.challengeScores.cameraScore);
        float speedRate = profile.ScoreToErrorRate(MiniGame1ChallengeType.SpeedConsistency, eval.challengeScores.speedScore);
        robotStats.SetErrorRates(driftRate, camRate, speedRate);
    }
}

