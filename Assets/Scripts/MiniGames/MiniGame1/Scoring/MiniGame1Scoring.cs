using UnityEngine;

public static class MiniGame1Scoring
{
    public static float ResponseTimeScore(MiniGame1LearningProfileSO profile, float responseTimeSeconds)
    {
        if (profile == null) return 50f;

        float tGood = Mathf.Max(0.01f, profile.responseTimeTargetSeconds);
        float tBad = Mathf.Max(tGood + 0.01f, profile.responseTimeWorstSeconds);

        if (responseTimeSeconds <= tGood) return 100f;
        if (responseTimeSeconds >= tBad) return 0f;

        float t = Mathf.InverseLerp(tBad, tGood, responseTimeSeconds);
        return Mathf.Clamp01(t) * 100f;
    }

    public static float PathAccuracyScore(MiniGame1LearningProfileSO profile, float averageLateralDistanceMeters)
    {
        if (profile == null) return 50f;
        float max = Mathf.Max(0.01f, profile.pathMaxAllowedDistance);
        return Mathf.Clamp01(1f - (averageLateralDistanceMeters / max)) * 100f;
    }
}

