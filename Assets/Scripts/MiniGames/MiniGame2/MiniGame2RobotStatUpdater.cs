public static class MiniGame2RobotStatUpdater
{
    public static void ApplyUpdateOnce(
        MiniGame2LearningProfileSO profile,
        RobotStatsSO robotStats,
        MiniGame2EvaluationResult eval)
    {
        if (profile == null || robotStats == null) return;

        if (eval.tier == MiniGameTier.Fail)
        {
            // Fail => no update (per design).
            return;
        }

        MiniGame2TierDeltas deltas = profile.GetTierDeltas(eval.tier);
        robotStats.ApplyMiniGame2Delta(deltas.energyEfficiencyDelta, deltas.pathAccuracyDelta, deltas.decisionConfidenceDelta);
    }
}

