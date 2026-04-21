using System;
using UnityEngine;

[Serializable]
public enum MiniGameTier
{
    Excellent,
    Good,
    Average,
    Fail
}

[Serializable]
public enum MiniGameMetricType
{
    PathAccuracy,
    CorrectionAccuracy,
    ResponseTime,
    SpeedConsistency,
    TargetAlignment
}

[Serializable]
public enum MiniGame1ChallengeType
{
    Drift,
    CameraAlignment,
    SpeedConsistency
}

[Serializable]
public struct WeightedMetric
{
    public MiniGameMetricType metric;
    [Range(0f, 1f)] public float weight;
}

[Serializable]
public struct MiniGame1ChallengeScores
{
    [Range(0f, 100f)] public float driftScore;
    [Range(0f, 100f)] public float cameraScore;
    [Range(0f, 100f)] public float speedScore;
}

[Serializable]
public struct MiniGameMetricScores
{
    [Range(0f, 100f)] public float pathAccuracy;
    [Range(0f, 100f)] public float correctionAccuracy;
    [Range(0f, 100f)] public float responseTime;
    [Range(0f, 100f)] public float speedConsistency;
    [Range(0f, 100f)] public float targetAlignment;
}

[Serializable]
public struct MiniGame1EvaluationResult
{
    [Range(0f, 100f)] public float finalScore;
    public MiniGameTier tier;
    public MiniGameMetricScores metricScores;
    public MiniGame1ChallengeScores challengeScores;
}

