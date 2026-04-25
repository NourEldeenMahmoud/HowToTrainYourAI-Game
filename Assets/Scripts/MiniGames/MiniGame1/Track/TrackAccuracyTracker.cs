using UnityEngine;

public class TrackAccuracyTracker : MonoBehaviour
{
    [SerializeField] private TrackProgress trackProgress;

    private float sumLateralDistance;
    private int samples;

    public void ResetTracking()
    {
        sumLateralDistance = 0f;
        samples = 0;
    }

    private void Update()
    {
        if (trackProgress == null || !trackProgress.HasTrack) return;
        sumLateralDistance += Mathf.Max(0f, trackProgress.GetLateralDistanceToCurrentSegment());
        samples++;
    }

    public float GetAverageLateralDistance()
    {
        return samples <= 0 ? 0f : (sumLateralDistance / samples);
    }
}

