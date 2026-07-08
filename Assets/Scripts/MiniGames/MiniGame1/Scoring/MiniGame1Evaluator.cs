public static class MiniGame1Evaluator
{
    public static MiniGame1EvaluationResult Evaluate(
        MiniGame1LearningProfileSO profile,
        MiniGame1RawMetrics raw,
        MiniGame1ChallengeScores challengeScores)
    {
        MiniGame1EvaluationInput input = new MiniGame1EvaluationInput
        {
            rawMetrics = raw,
            driftLeft = new MiniGame1ChallengeRunMetrics { hasValue = true },
            camera = new MiniGame1ChallengeRunMetrics { hasValue = true },
            speed = new MiniGame1ChallengeRunMetrics { hasValue = true }
        };

        MiniGame1EvaluationResult result = MiniGame1ScoringEngine.Evaluate(profile, input);
        result.challengeScores = challengeScores;
        result.finalScore = profile != null && profile.useDisplayedChallengeScoresForFinal
            ? ComputeWeightedChallengeFinal(profile, challengeScores)
            : result.finalScore;
        result.tier = profile != null
            ? profile.GetTier(result.finalScore)
            : (result.finalScore >= 50f ? MiniGameTier.Average : MiniGameTier.Fail);
        return result;
    }

    private static float ComputeWeightedChallengeFinal(MiniGame1LearningProfileSO profile, MiniGame1ChallengeScores scores)
    {
        if (profile == null || profile.weightedChallengeScores == null || profile.weightedChallengeScores.Length == 0)
            return (scores.driftScore + scores.cameraScore + scores.speedScore) / 3f;

        float sum = 0f;
        float weightSum = 0f;
        foreach (WeightedChallengeScore weightedScore in profile.weightedChallengeScores)
        {
            float score = weightedScore.challenge switch
            {
                MiniGame1ChallengeType.Drift => scores.driftScore,
                MiniGame1ChallengeType.CameraAlignment => scores.cameraScore,
                MiniGame1ChallengeType.SpeedConsistency => scores.speedScore,
                _ => 0f
            };

            sum += score * weightedScore.weight;
            weightSum += weightedScore.weight;
        }

        return weightSum <= 0.0001f ? 0f : UnityEngine.Mathf.Clamp(sum / weightSum, 0f, 100f);
    }
}
