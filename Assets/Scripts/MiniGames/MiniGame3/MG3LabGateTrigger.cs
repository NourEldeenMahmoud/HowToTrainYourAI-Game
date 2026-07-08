using UnityEngine;

[DisallowMultipleComponent]
public class MG3LabGateTrigger : MonoBehaviour
{
    [Header("Robot Filter")]
    [SerializeField] private Transform robotRoot;
    [SerializeField] private string robotTag = "Robot";

    [Header("Transition")]
    [SerializeField] private string miniGame3SceneName = "MiniGame 3";
    [SerializeField, Min(0.05f)] private float fadeDurationSeconds = 1f;
    [SerializeField] private bool singleUse = true;

    [Header("Debug")]
    [SerializeField] private bool enableLogs;

    private bool consumed;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger != null && !trigger.isTrigger)
            trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (singleUse && consumed)
            return;

        if (!IsRobotCollider(other))
        {
            Log($"Ignored non-robot trigger: {other.name}");
            return;
        }

        if (string.IsNullOrWhiteSpace(miniGame3SceneName))
        {
            Debug.LogError("[MG3LabGateTrigger] MiniGame 3 scene name is empty. Cannot transition.", this);
            return;
        }

        consumed = true;
        Log($"Robot entered lab gate. Loading {miniGame3SceneName}.");
        SceneTransitionFader.TransitionToScene(miniGame3SceneName, -1, fadeDurationSeconds);
    }

    private bool IsRobotCollider(Collider other)
    {
        if (other == null)
            return false;

        if (robotRoot != null)
        {
            Transform t = other.transform;
            if (t == robotRoot || t.IsChildOf(robotRoot) || robotRoot.IsChildOf(t))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(robotTag))
        {
            if (other.CompareTag(robotTag))
                return true;

            Transform root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
            if (root != null && root.CompareTag(robotTag))
                return true;
        }

        return other.GetComponentInParent<RobotMovement>() != null;
    }

    private void Log(string message)
    {
        if (enableLogs)
            Debug.Log("[MG3LabGateTrigger] " + message, this);
    }
}
