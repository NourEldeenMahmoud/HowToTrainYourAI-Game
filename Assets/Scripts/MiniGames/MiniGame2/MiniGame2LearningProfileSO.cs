using UnityEngine;

[CreateAssetMenu(menuName = "HowToTrainYourAI/MiniGames/MiniGame2LearningProfileSO", fileName = "MiniGame2LearningProfileSO")]
public class MiniGame2LearningProfileSO : ScriptableObject
{
    [Header("Pass / Tier Rules")]
    [Range(0f, 100f)] public float passScore = 50f;
    [Tooltip("MiniGame 2 tiers: 85–100 Excellent, 70–84 Good, 50–69 Average, <50 Fail")]
    [Range(0f, 100f)] public float excellentMinScore = 85f;
    [Range(0f, 100f)] public float goodMinScore = 70f;
    [Range(0f, 100f)] public float averageMinScore = 50f;

    [Header("Final Score Weights (sum should be 1.0)")]
    [Range(0f, 1f)] public float energyEfficiencyWeight = 0.40f;
    [Range(0f, 1f)] public float pathEfficiencyWeight = 0.35f;
    [Range(0f, 1f)] public float collisionSafetyWeight = 0.25f;

    [Header("Collision Safety")]
    [Tooltip("How many points are subtracted per collision from the collision safety score (starting from 100).")]
    [Range(0f, 100f)] public float collisionPenaltyPerHit = 10f;

    [Header("Tier Deltas")]
    public MiniGame2TierDeltas excellentDeltas = new MiniGame2TierDeltas
    {
        energyEfficiencyDelta = 0.15f,
        pathAccuracyDelta = 0.15f,
        decisionConfidenceDelta = 0.10f
    };
    public MiniGame2TierDeltas goodDeltas = new MiniGame2TierDeltas
    {
        energyEfficiencyDelta = 0.10f,
        pathAccuracyDelta = 0.10f,
        decisionConfidenceDelta = 0.07f
    };
    public MiniGame2TierDeltas averageDeltas = new MiniGame2TierDeltas
    {
        energyEfficiencyDelta = 0.05f,
        pathAccuracyDelta = 0.05f,
        decisionConfidenceDelta = 0.03f
    };

    public MiniGameTier GetTier(float finalScore)
    {
        if (finalScore < passScore) return MiniGameTier.Fail;
        if (finalScore >= excellentMinScore) return MiniGameTier.Excellent;
        if (finalScore >= goodMinScore) return MiniGameTier.Good;
        if (finalScore >= averageMinScore) return MiniGameTier.Average;
        return MiniGameTier.Fail;
    }

    public MiniGame2TierDeltas GetTierDeltas(MiniGameTier tier)
    {
        return tier switch
        {
            MiniGameTier.Excellent => excellentDeltas,
            MiniGameTier.Good => goodDeltas,
            MiniGameTier.Average => averageDeltas,
            _ => default
        };
    }

    public float ComputeEnergyEfficiencyScore(float idealEnergy, float actualEnergy)
    {
        if (idealEnergy <= 0f || actualEnergy <= 0f) return 0f;
        return Mathf.Clamp((idealEnergy / actualEnergy) * 100f, 0f, 100f);
    }

    public float ComputePathEfficiencyScore(int idealSteps, int actualSteps)
    {
        if (idealSteps <= 0 || actualSteps <= 0) return 0f;
        return Mathf.Clamp(((float)idealSteps / actualSteps) * 100f, 0f, 100f);
    }

    public float ComputeCollisionSafetyScore(int collisionCount)
    {
        float score = 100f - Mathf.Max(0, collisionCount) * collisionPenaltyPerHit;
        return Mathf.Clamp(score, 0f, 100f);
    }

    public float ComputeFinalScore(float energyScore0To100, float pathScore0To100, float collisionScore0To100)
    {
        return Mathf.Clamp(
            energyScore0To100 * energyEfficiencyWeight +
            pathScore0To100 * pathEfficiencyWeight +
            collisionScore0To100 * collisionSafetyWeight,
            0f,
            100f
        );
    }
}

