using UnityEngine;

public static class MiniGame1ScoringEngine
{
    public static MiniGame1EvaluationResult Evaluate(MiniGame1LearningProfileSO profile, MiniGame1EvaluationInput input)
    {
        MiniGame1ChallengeScores challengeScores = ComputeChallengeScores(profile, input);
        MiniGameMetricScores metricScores = ComputeMetricScores(profile, input.rawMetrics);

        float finalScore = profile != null && profile.useDisplayedChallengeScoresForFinal
            ? ComputeWeightedChallengeFinal(profile, challengeScores)
            : ComputeWeightedMetricFinal(profile, metricScores);

        MiniGameTier tier = profile != null
            ? profile.GetTier(finalScore)
            : (finalScore >= 50f ? MiniGameTier.Average : MiniGameTier.Fail);

        return new MiniGame1EvaluationResult
        {
            finalScore = finalScore,
            tier = tier,
            metricScores = metricScores,
            challengeScores = challengeScores
        };
    }

    public static MiniGame1ChallengeScores ComputeChallengeScores(MiniGame1LearningProfileSO profile, MiniGame1EvaluationInput input)
    {
        float driftScore = 0f;

        if (input.driftLeft.hasValue && input.driftRight.hasValue)
        {
            driftScore = (ScoreDrift(profile, input.driftLeft) + ScoreDrift(profile, input.driftRight)) * 0.5f;
        }
        else if (input.driftLeft.hasValue)
        {
            driftScore = ScoreDrift(profile, input.driftLeft);
        }
        else if (input.driftRight.hasValue)
        {
            driftScore = ScoreDrift(profile, input.driftRight);
        }

        return new MiniGame1ChallengeScores
        {
            driftScore = driftScore,
            cameraScore = input.camera.hasValue ? ScoreCamera(profile, input.camera) : 0f,
            speedScore = input.speed.hasValue ? ScoreSpeed(profile, input.speed) : 0f
        };
    }

    public static MiniGameMetricScores ComputeMetricScores(MiniGame1LearningProfileSO profile, MiniGame1RawMetrics raw)
    {
        return new MiniGameMetricScores
        {
            pathAccuracy = MiniGame1Scoring.PathAccuracyScore(profile, raw.averageLateralDistanceMeters),
            correctionAccuracy = CorrectionAccuracyScore(profile, raw),
            responseTime = MiniGame1Scoring.ResponseTimeScore(profile, raw.GetAverageResponseTime()),
            speedConsistency = SpeedConsistencyScore(profile, raw),
            targetAlignment = TargetAlignmentScore(profile, raw)
        };
    }

    public static float ScoreDrift(MiniGame1LearningProfileSO profile, MiniGame1ChallengeRunMetrics metrics)
    {
        float responseScore = MiniGame1Scoring.ResponseTimeScore(profile, metrics.responseTimeSeconds);
        float worstError = profile != null ? profile.correctionWorstErrorDeg : 45f;
        float errorScore = Mathf.Clamp01(1f - (metrics.averageErrorDeg / Mathf.Max(0.1f, worstError))) * 100f;
        return Mathf.Clamp01((0.55f * (responseScore / 100f)) + (0.45f * (errorScore / 100f))) * 100f;
    }

    public static float ScoreCamera(MiniGame1LearningProfileSO profile, MiniGame1ChallengeRunMetrics metrics)
    {
        float responseScore = MiniGame1Scoring.ResponseTimeScore(profile, metrics.responseTimeSeconds);
        float worstError = profile != null ? profile.cameraWorstErrorDeg : 30f;
        float alignmentScore = Mathf.Clamp01(1f - (metrics.averageErrorDeg / Mathf.Max(0.1f, worstError))) * 100f;
        return Mathf.Clamp01((0.5f * (responseScore / 100f)) + (0.5f * (alignmentScore / 100f))) * 100f;
    }

    public static float ScoreSpeed(MiniGame1LearningProfileSO profile, MiniGame1ChallengeRunMetrics metrics)
    {
        float target = Mathf.Max(0.01f, metrics.averageSpeed > 0.01f ? metrics.averageSpeed : metrics.speedTarget);
        float worstRatio = profile != null ? profile.speedWorstStdDevTargetRatio : 0.5f;
        float worst = Mathf.Max(0.01f, target * Mathf.Max(0.01f, worstRatio));
        return Mathf.Clamp01(1f - (metrics.speedStdDev / worst)) * 100f;
    }

    private static float ComputeWeightedChallengeFinal(MiniGame1LearningProfileSO profile, MiniGame1ChallengeScores scores)
    {
        if (profile == null || profile.weightedChallengeScores == null || profile.weightedChallengeScores.Length == 0)
        {
            return (scores.driftScore + scores.cameraScore + scores.speedScore) / 3f;
        }

        float sum = 0f;
        float weightSum = 0f;
        foreach (WeightedChallengeScore weightedScore in profile.weightedChallengeScores)
        {
            float score = GetChallengeScore(scores, weightedScore.challenge);
            sum += score * weightedScore.weight;
            weightSum += weightedScore.weight;
        }

        if (weightSum <= 0.0001f) return 0f;
        return Mathf.Clamp(sum / weightSum, 0f, 100f);
    }

    private static float ComputeWeightedMetricFinal(MiniGame1LearningProfileSO profile, MiniGameMetricScores metrics)
    {
        if (profile == null || profile.weightedMetrics == null || profile.weightedMetrics.Length == 0)
        {
            return (metrics.pathAccuracy + metrics.correctionAccuracy + metrics.responseTime) / 3f;
        }

        float sum = 0f;
        float weightSum = 0f;
        foreach (WeightedMetric weightedMetric in profile.weightedMetrics)
        {
            float score = GetMetricScore(metrics, weightedMetric.metric);
            sum += score * weightedMetric.weight;
            weightSum += weightedMetric.weight;
        }

        if (weightSum <= 0.0001f) return 0f;
        return Mathf.Clamp(sum / weightSum, 0f, 100f);
    }

    private static float CorrectionAccuracyScore(MiniGame1LearningProfileSO profile, MiniGame1RawMetrics raw)
    {
        float worst = profile != null ? profile.correctionWorstErrorDeg : 45f;
        return Mathf.Clamp01(1f - (raw.GetAverageCorrectionErrorDeg() / Mathf.Max(0.1f, worst))) * 100f;
    }

    private static float TargetAlignmentScore(MiniGame1LearningProfileSO profile, MiniGame1RawMetrics raw)
    {
        float worst = profile != null ? profile.cameraWorstErrorDeg : 30f;
        return Mathf.Clamp01(1f - (raw.GetAverageCameraErrorDeg() / Mathf.Max(0.1f, worst))) * 100f;
    }

    private static float SpeedConsistencyScore(MiniGame1LearningProfileSO profile, MiniGame1RawMetrics raw)
    {
        float target = Mathf.Max(0.01f, raw.speedTarget);
        float worstRatio = profile != null ? profile.speedWorstStdDevTargetRatio : 0.5f;
        float worst = target * Mathf.Max(0.01f, worstRatio);
        return Mathf.Clamp01(1f - (raw.speedStdDev / Mathf.Max(0.01f, worst))) * 100f;
    }

    private static float GetMetricScore(MiniGameMetricScores metrics, MiniGameMetricType type)
    {
        return type switch
        {
            MiniGameMetricType.PathAccuracy => metrics.pathAccuracy,
            MiniGameMetricType.CorrectionAccuracy => metrics.correctionAccuracy,
            MiniGameMetricType.ResponseTime => metrics.responseTime,
            MiniGameMetricType.SpeedConsistency => metrics.speedConsistency,
            MiniGameMetricType.TargetAlignment => metrics.targetAlignment,
            _ => 0f
        };
    }

    private static float GetChallengeScore(MiniGame1ChallengeScores scores, MiniGame1ChallengeType type)
    {
        return type switch
        {
            MiniGame1ChallengeType.Drift => scores.driftScore,
            MiniGame1ChallengeType.CameraAlignment => scores.cameraScore,
            MiniGame1ChallengeType.SpeedConsistency => scores.speedScore,
            _ => 0f
        };
    }
}
