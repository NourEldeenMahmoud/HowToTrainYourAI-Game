using System;
using UnityEngine;

public enum MiniGame2Phase
{
    Idle,
    Planning,
    RobotMoving,
    Completed
}

[Serializable]
public struct MiniGame2TierDeltas
{
    [Header("Core stat deltas (applied once at end, clamped 0..1)")]
    public float energyEfficiencyDelta;
    public float pathAccuracyDelta;
    public float decisionConfidenceDelta;
}

[Serializable]
public struct MiniGame2EvaluationResult
{
    [Range(0f, 100f)] public float finalScore;
    public MiniGameTier tier; // reuse enum from MiniGame1Types.cs

    [Header("Component scores (0..100)")]
    [Range(0f, 100f)] public float energyEfficiencyScore;   // 40%
    [Range(0f, 100f)] public float pathEfficiencyScore;     // 35%
    [Range(0f, 100f)] public float collisionSafetyScore;    // 25%

    [Header("Raw totals")]
    public float actualEnergy;
    public float idealEnergy;
    public int collisionCount;
    public int actualStepCount;
    public int idealStepCount;
}

