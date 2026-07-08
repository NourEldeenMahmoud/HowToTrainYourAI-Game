using System.Collections.Generic;
using UnityEngine;

public class TrackProgress : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform subject;
    [SerializeField] private Transform waypointsRoot;

    [Header("Settings")]
    [Tooltip("Distance to consider a waypoint reached.")]
    [Min(0.01f)] public float waypointReachDistance = 1.0f;

    private readonly List<Transform> waypoints = new List<Transform>();
    private int currentIndex;

    public int CurrentWaypointIndex => currentIndex;
    public int WaypointCount => waypoints.Count;
    public bool HasTrack => waypoints.Count >= 2;

    private void Awake()
    {
        RebuildWaypoints();
    }

    public void RebuildWaypoints()
    {
        waypoints.Clear();
        currentIndex = 0;

        if (waypointsRoot == null) return;
        for (int i = 0; i < waypointsRoot.childCount; i++)
        {
            Transform child = waypointsRoot.GetChild(i);
            waypoints.Add(child);
        }
    }

    private void Update()
    {
        if (!HasTrack || subject == null) return;

        Transform next = waypoints[Mathf.Clamp(currentIndex + 1, 0, waypoints.Count - 1)];
        float d = Vector3.Distance(subject.position, next.position);
        if (d <= waypointReachDistance && currentIndex < waypoints.Count - 2)
        {
            currentIndex++;
        }
    }

    public Vector3 GetCurrentSegmentDirection()
    {
        if (!HasTrack) return subject != null ? subject.forward : Vector3.forward;

        Transform a = waypoints[Mathf.Clamp(currentIndex, 0, waypoints.Count - 1)];
        Transform b = waypoints[Mathf.Clamp(currentIndex + 1, 0, waypoints.Count - 1)];
        Vector3 dir = (b.position - a.position);
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
    }

    public float GetLateralDistanceToCurrentSegment()
    {
        if (!HasTrack || subject == null) return 0f;

        Transform a = waypoints[Mathf.Clamp(currentIndex, 0, waypoints.Count - 1)];
        Transform b = waypoints[Mathf.Clamp(currentIndex + 1, 0, waypoints.Count - 1)];

        Vector3 p = subject.position;
        Vector3 aPos = a.position;
        Vector3 bPos = b.position;

        // Flatten on Y to avoid elevation noise.
        p.y = 0f;
        aPos.y = 0f;
        bPos.y = 0f;

        Vector3 ab = bPos - aPos;
        float abLenSqr = ab.sqrMagnitude;
        if (abLenSqr < 0.0001f) return Vector3.Distance(p, aPos);

        float t = Vector3.Dot(p - aPos, ab) / abLenSqr;
        t = Mathf.Clamp01(t);
        Vector3 closest = aPos + ab * t;
        return Vector3.Distance(p, closest);
    }

    public bool IsFinished()
    {
        if (!HasTrack || subject == null) return false;
        return currentIndex >= waypoints.Count - 2 &&
               Vector3.Distance(subject.position, waypoints[waypoints.Count - 1].position) <= waypointReachDistance;
    }
}

