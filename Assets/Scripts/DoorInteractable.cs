using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MG1ToMG2FlowCoordinator flowCoordinator;
    [SerializeField] private Transform robotRoot;

    [Header("Robot Filter")]
    [SerializeField] private string robotTag = "Robot";

    [Header("Behavior")]
    [SerializeField] private bool reactToTrigger = true;
    [SerializeField] private bool reactToCollision = true;
    [SerializeField] private bool pollRobotProximity = true;
    [SerializeField, Min(0.01f)] private float proximityThreshold = 0.25f;
    [SerializeField] private bool singleUse = true;
    [SerializeField] private bool enableLogs;

    private bool consumed;
    private Collider doorCollider;

    private void Awake()
    {
        doorCollider = GetComponent<Collider>();

        if (flowCoordinator == null)
            flowCoordinator = FindFirstObjectByType<MG1ToMG2FlowCoordinator>();
    }

    private void Update()
    {
        if (!pollRobotProximity)
            return;

        if (singleUse && consumed)
            return;

        if (doorCollider == null)
            return;

        Transform robot = ResolveRobotTransform();
        if (robot == null)
            return;

        Vector3 closest = doorCollider.ClosestPoint(robot.position);
        float sqrDistance = (closest - robot.position).sqrMagnitude;
        if (sqrDistance <= proximityThreshold * proximityThreshold)
            TryStartTransition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (reactToTrigger)
            TryHandleDoorPass(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (reactToCollision)
            TryHandleDoorPass(collision.collider);
    }

    private void TryHandleDoorPass(Collider other)
    {
        if (other == null)
            return;

        if (singleUse && consumed)
            return;

        if (!IsRobotCollider(other))
            return;

        TryStartTransition();
    }

    private void TryStartTransition()
    {
        if (singleUse && consumed)
            return;

        if (flowCoordinator == null)
            flowCoordinator = FindFirstObjectByType<MG1ToMG2FlowCoordinator>();

        if (flowCoordinator == null)
        {
            Log("Robot touched door but flow coordinator was not found.");
            return;
        }

        bool started = flowCoordinator.TryStartStorageDoorTransition();
        if (!started)
        {
            Log("Robot touched door but transition is not available yet.");
            return;
        }

        consumed = true;
        Log("Robot entered storage door; transition started.");
    }

    private Transform ResolveRobotTransform()
    {
        if (robotRoot != null)
            return robotRoot;

        RobotMovement movement = FindFirstObjectByType<RobotMovement>();
        if (movement != null)
            return movement.transform;

        GameObject robot = GameObject.FindWithTag(robotTag);
        return robot != null ? robot.transform : null;
    }

    private bool IsRobotCollider(Collider other)
    {
        if (robotRoot != null)
        {
            Transform t = other.transform;
            if (t == robotRoot || t.IsChildOf(robotRoot) || robotRoot.IsChildOf(t))
                return true;
        }

        if (other.CompareTag(robotTag))
            return true;

        Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
        if (root != null && root.CompareTag(robotTag))
            return true;

        if (other.GetComponentInParent<RobotMovement>() != null)
            return true;

        return false;
    }

    private void Log(string message)
    {
        if (enableLogs)
            Debug.Log("[DoorInteractable] " + message, this);
    }
}
