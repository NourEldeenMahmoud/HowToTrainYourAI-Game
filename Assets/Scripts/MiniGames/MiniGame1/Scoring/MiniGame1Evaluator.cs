using System.Collections.Generic;
using UnityEngine;

public static class MiniGame1Evaluator
{
    public static MiniGame1EvaluationResult Evaluate(
        MiniGame1LearningProfileSO profile,
        MiniGame1RawMetrics raw,
        MiniGame1ChallengeScores challengeScores)
    {
        MiniGameMetricScores metricScores = new MiniGameMetricScores
        {
            pathAccuracy = MiniGame1Scoring.PathAccuracyScore(profile, raw.averageLateralDistanceMeters),
            correctionAccuracy = CorrectionAccuracyScore(raw),
            responseTime = MiniGame1Scoring.ResponseTimeScore(profile, raw.GetAverageResponseTime()),
            speedConsistency = SpeedConsistencyScore(raw),
            targetAlignment = TargetAlignmentScore(raw)
        };

        float finalScore = ComputeWeightedFinal(profile, metricScores);
        MiniGameTier tier = profile != null ? profile.GetTier(finalScore) : (finalScore >= 50f ? MiniGameTier.Average : MiniGameTier.Fail);

        return new MiniGame1EvaluationResult
        {
            finalScore = finalScore,
            tier = tier,
            metricScores = metricScores,
            challengeScores = challengeScores
        };
    }

    private static float ComputeWeightedFinal(MiniGame1LearningProfileSO profile, MiniGameMetricScores metrics)
    {
        if (profile == null || profile.weightedMetrics == null || profile.weightedMetrics.Length == 0)
        {
            // Fallback: average core three metrics
            return (metrics.pathAccuracy + metrics.correctionAccuracy + metrics.responseTime) / 3f;
        }

        float sum = 0f;
        float wsum = 0f;
        foreach (WeightedMetric wm in profile.weightedMetrics)
        {
            float ms = GetMetricScore(metrics, wm.metric);
            sum += ms * wm.weight;
            wsum += wm.weight;
        }

        if (wsum <= 0.0001f) return 0f;
        return Mathf.Clamp(sum / wsum, 0f, 100f);
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

    private static float CorrectionAccuracyScore(MiniGame1RawMetrics raw)
    {
        // Lower avg correction error => higher score.
        float avgErr = raw.GetAverageCorrectionErrorDeg();
        // 0 deg => 100, 45 => 0
        return Mathf.Clamp01(1f - (avgErr / 45f)) * 100f;
    }

    private static float TargetAlignmentScore(MiniGame1RawMetrics raw)
    {
        float avgErr = raw.GetAverageCameraErrorDeg();
        // 0 deg => 100, 30 => 0
        return Mathf.Clamp01(1f - (avgErr / 30f)) * 100f;
    }

    private static float SpeedConsistencyScore(MiniGame1RawMetrics raw)
    {
        float target = Mathf.Max(0.01f, raw.speedTarget);
        float worst = target * 0.5f;
        return Mathf.Clamp01(1f - (raw.speedStdDev / Mathf.Max(0.01f, worst))) * 100f;
    }
}

