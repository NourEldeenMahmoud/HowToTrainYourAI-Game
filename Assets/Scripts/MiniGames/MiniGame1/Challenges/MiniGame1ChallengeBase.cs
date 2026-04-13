using UnityEngine;

public abstract class MiniGame1ChallengeBase : MonoBehaviour
{
    public abstract MiniGame1ChallengeType ChallengeType { get; }

    public virtual void BeginChallenge() { }
    public virtual void EndChallenge() { }

    /// <summary>
    /// Returns a 0..100 score for this challenge.
    /// </summary>
    public abstract float GetScore0To100(MiniGame1LearningProfileSO profile);

    /// <summary>
    /// Optional: expose additional metric signals to the evaluator.
    /// </summary>
    public virtual void ContributeToMetrics(ref MiniGame1RawMetrics raw) { }
}

