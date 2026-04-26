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
    [Range(0f, 1f)] public float pathEfficiencyWeight = 0.60f;

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

        float lower = Mathf.Min(idealEnergy, actualEnergy);
        float higher = Mathf.Max(idealEnergy, actualEnergy);
        return Mathf.Clamp((lower / higher) * 100f, 0f, 100f);
    }

    public float ComputePathEfficiencyScore(int idealSteps, int actualSteps)
    {
        if (idealSteps <= 0 || actualSteps <= 0) return 0f;

        int lower = Mathf.Min(idealSteps, actualSteps);
        int higher = Mathf.Max(idealSteps, actualSteps);
        return Mathf.Clamp(((float)lower / higher) * 100f, 0f, 100f);
    }

    public float ComputeFinalScore(float energyScore0To100, float pathScore0To100)
    {
        float energyWeight = Mathf.Max(0f, energyEfficiencyWeight);
        float pathWeight = Mathf.Max(0f, pathEfficiencyWeight);
        float sum = energyWeight + pathWeight;
        if (sum <= 0f)
        {
            energyWeight = 0.5f;
            pathWeight = 0.5f;
            sum = 1f;
        }

        return Mathf.Clamp((energyScore0To100 * energyWeight + pathScore0To100 * pathWeight) / sum, 0f, 100f);
    }
}
