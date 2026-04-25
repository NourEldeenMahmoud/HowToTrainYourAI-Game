using System;
using UnityEngine;

[Serializable]
public struct MiniGame1TierDeltas
{
    [Header("Core stat deltas (applied once at end, clamped 0..1)")]
    public float stabilityDelta;
    public float pathAccuracyDelta;
    public float inputResponsivenessDelta;
}

[CreateAssetMenu(menuName = "HowToTrainYourAI/MiniGames/MiniGame1LearningProfileSO", fileName = "MiniGame1LearningProfileSO")]
public class MiniGame1LearningProfileSO : ScriptableObject
{
    [Header("Pass / Tier Rules")]
    [Tooltip("Minimum final score required to pass MG1 and apply robot stat improvements. Suggested: 50.")]
    [Range(0f, 100f)] public float passScore = 50f;

    [Tooltip("MiniGame 1 tiers: 90–100 Excellent, 70–89 Good, 50–69 Average, <50 Fail")]
    [Range(0f, 100f)] public float excellentMinScore = 90f;
    [Tooltip("Minimum final score for Good tier. Suggested: 70.")]
    [Range(0f, 100f)] public float goodMinScore = 70f;
    [Tooltip("Minimum final score for Average tier. Usually matches Pass Score. Suggested: 50.")]
    [Range(0f, 100f)] public float averageMinScore = 50f;

    [Header("Final Score Source")]
    [Tooltip("When enabled, final score uses the same Drift/Camera/Speed scores shown on the result screen. Recommended to avoid confusing mismatches.")]
    public bool useDisplayedChallengeScoresForFinal = true;

    [Header("Displayed Challenge Weights (sum should be 1.0)")]
    [Tooltip("Controls final score when Use Displayed Challenge Scores For Final is enabled. These are the same categories shown in the result UI.")]
    public WeightedChallengeScore[] weightedChallengeScores = new[]
    {
        new WeightedChallengeScore { challenge = MiniGame1ChallengeType.Drift, weight = 0.40f },
        new WeightedChallengeScore { challenge = MiniGame1ChallengeType.CameraAlignment, weight = 0.25f },
        new WeightedChallengeScore { challenge = MiniGame1ChallengeType.SpeedConsistency, weight = 0.35f },
    };

    [Header("Raw Metric Weights (legacy, sum should be 1.0)")]
    [Tooltip("Legacy final score formula. Used only when Use Displayed Challenge Scores For Final is disabled.")]
    public WeightedMetric[] weightedMetrics = new[]
    {
        new WeightedMetric { metric = MiniGameMetricType.PathAccuracy, weight = 0.40f },
        new WeightedMetric { metric = MiniGameMetricType.CorrectionAccuracy, weight = 0.35f },
        new WeightedMetric { metric = MiniGameMetricType.ResponseTime, weight = 0.25f },
    };

    [Header("Tier Deltas")]
    [Tooltip("Robot stat gains when final score reaches Excellent. Suggested: 0.10-0.15.")]
    public MiniGame1TierDeltas excellentDeltas = new MiniGame1TierDeltas { stabilityDelta = 0.15f, pathAccuracyDelta = 0.15f, inputResponsivenessDelta = 0.10f };
    [Tooltip("Robot stat gains when final score reaches Good. Suggested: 0.07-0.10.")]
    public MiniGame1TierDeltas goodDeltas = new MiniGame1TierDeltas { stabilityDelta = 0.10f, pathAccuracyDelta = 0.10f, inputResponsivenessDelta = 0.07f };
    [Tooltip("Robot stat gains when final score reaches Average/pass. Suggested: 0.03-0.05.")]
    public MiniGame1TierDeltas averageDeltas = new MiniGame1TierDeltas { stabilityDelta = 0.05f, pathAccuracyDelta = 0.05f, inputResponsivenessDelta = 0.03f };

    [Header("Tolerances / Normalization")]
    [Tooltip("Max lateral distance from path considered in scoring (meters).")]
    [Min(0.01f)] public float pathMaxAllowedDistance = 1.0f;
    [Tooltip("Target time (seconds) considered as 100 score for response time; slower reduces score.")]
    [Min(0.01f)] public float responseTimeTargetSeconds = 0.35f;
    [Tooltip("Time (seconds) where response time score reaches 0.")]
    [Min(0.01f)] public float responseTimeWorstSeconds = 3.0f;
    [Tooltip("Average drift/correction error that scores 0. Suggested: 35-60 degrees.")]
    [Min(0.1f)] public float correctionWorstErrorDeg = 45f;
    [Tooltip("Average camera pitch error that scores 0. Suggested: 20-40 degrees.")]
    [Min(0.1f)] public float cameraWorstErrorDeg = 30f;
    [Tooltip("Speed standard deviation as a fraction of target speed that scores 0. 0.5 means std dev is 50% of target speed. Suggested: 0.4-0.7.")]
    [Min(0.01f)] public float speedWorstStdDevTargetRatio = 0.5f;

    [Header("Challenge Score -> Error Rate (0..1)")]
    [Tooltip("Maps drift challenge score (0..100) to drift error rate (0..1). Lower score => higher error rate.")]
    public AnimationCurve driftScoreToErrorRate = DefaultInverseCurve();
    [Tooltip("Maps camera challenge score (0..100) to camera error rate (0..1). Lower score => higher error rate.")]
    public AnimationCurve cameraScoreToErrorRate = DefaultInverseCurve();
    [Tooltip("Maps speed challenge score (0..100) to speed error rate (0..1). Lower score => higher error rate.")]
    public AnimationCurve speedScoreToErrorRate = DefaultInverseCurve();

    public MiniGameTier GetTier(float finalScore)
    {
        if (finalScore < passScore) return MiniGameTier.Fail;
        if (finalScore >= excellentMinScore) return MiniGameTier.Excellent;
        if (finalScore >= goodMinScore) return MiniGameTier.Good;
        if (finalScore >= averageMinScore) return MiniGameTier.Average;
        return MiniGameTier.Fail;
    }

    public MiniGame1TierDeltas GetTierDeltas(MiniGameTier tier)
    {
        return tier switch
        {
            MiniGameTier.Excellent => excellentDeltas,
            MiniGameTier.Good => goodDeltas,
            MiniGameTier.Average => averageDeltas,
            _ => default
        };
    }

    public float ScoreToErrorRate(MiniGame1ChallengeType type, float score0To100)
    {
        float t = Mathf.Clamp(score0To100, 0f, 100f);
        AnimationCurve curve = type switch
        {
            MiniGame1ChallengeType.Drift => driftScoreToErrorRate,
            MiniGame1ChallengeType.CameraAlignment => cameraScoreToErrorRate,
            MiniGame1ChallengeType.SpeedConsistency => speedScoreToErrorRate,
            _ => null
        };

        if (curve == null || curve.length == 0)
        {
            // Safe default: 0.5 baseline.
            return 0.5f;
        }

        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private static AnimationCurve DefaultInverseCurve()
    {
        // 0 score => 0.95 error rate, 50 => 0.5, 100 => 0.05
        return new AnimationCurve(
            new Keyframe(0f, 0.95f),
            new Keyframe(50f, 0.50f),
            new Keyframe(100f, 0.05f)
        );
    }
}
