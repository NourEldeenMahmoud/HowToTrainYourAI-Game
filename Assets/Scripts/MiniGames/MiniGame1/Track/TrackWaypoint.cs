using UnityEngine;

public class TrackWaypoint : MonoBehaviour
{
    [Tooltip("Optional visual radius for gizmo drawing.")]
    [Min(0.01f)] public float gizmoRadius = 0.25f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}

