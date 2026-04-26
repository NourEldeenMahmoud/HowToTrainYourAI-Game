using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MG2ReturnToNourSpawner : MonoBehaviour
{
    private const string NourSceneName = "Nour";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnSceneLoad()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.name.Equals(NourSceneName, StringComparison.OrdinalIgnoreCase))
            return;

        if (FindFirstObjectByType<MG2ReturnToNourSpawner>() != null)
            return;

        GameObject go = new GameObject("MG2ReturnToNourSpawner");
        go.AddComponent<MG2ReturnToNourSpawner>();
    }

    private void Start()
    {
        TryApplySpawn();
    }

    private void TryApplySpawn()
    {
        if (!GameSessionFlowFlags.TryConsumeMiniGame2ReturnSpawn(out GameSessionFlowFlags.MiniGame2ReturnSpawnRequest request))
        {
            Destroy(gameObject);
            return;
        }

        Transform anchor = ResolveAnchor(request.primaryAnchorName, request.fallbackAnchorName);
        if (anchor == null)
        {
            Debug.LogWarning("[MG2->Nour] Spawn anchor not found. Keeping default scene placement.", this);
            Destroy(gameObject);
            return;
        }

        Transform player = ResolvePlayerTransform();
        Transform robot = ResolveRobotTransform();

        Vector3 playerWorld = anchor.TransformPoint(request.playerLocalOffset);
        Vector3 robotWorld = anchor.TransformPoint(request.robotLocalOffset);

        if (player != null)
            PlaceActor(player, playerWorld, request.faceTowardAnchor, anchor.position);
        else
            Debug.LogWarning("[MG2->Nour] Player transform not found for spawn placement.", this);

        if (robot != null)
            PlaceActor(robot, robotWorld, request.faceTowardAnchor, anchor.position);
        else
            Debug.LogWarning("[MG2->Nour] Robot transform not found for spawn placement.", this);

        ControlManager controlManager = FindFirstObjectByType<ControlManager>();
        if (controlManager != null)
            controlManager.ForcePlayerControlState();

        Destroy(gameObject);
    }

    private static Transform ResolveAnchor(string primaryName, string fallbackName)
    {
        Transform primary = FindTransformByNameIncludingInactive(primaryName);
        if (primary != null)
            return primary;

        Transform fallback = FindTransformByNameIncludingInactive(fallbackName);
        if (fallback != null)
            return fallback;

        return FindTransformByNameFragmentIncludingInactive("Warehouse_Door");
    }

    private static Transform ResolvePlayerTransform()
    {
        GameObject byTag = GameObject.FindWithTag("Player");
        if (byTag != null)
            return byTag.transform;

        GameObject byName = GameObject.Find("Player");
        if (byName != null)
            return byName.transform;

        return null;
    }

    private static Transform ResolveRobotTransform()
    {
        RobotMovement robotMovement = FindFirstObjectByType<RobotMovement>();
        if (robotMovement != null)
            return robotMovement.transform;

        GameObject byTag = GameObject.FindWithTag("Robot");
        if (byTag != null)
            return byTag.transform;

        return null;
    }

    private static void PlaceActor(Transform actor, Vector3 position, bool faceTowardAnchor, Vector3 anchorPosition)
    {
        if (actor == null)
            return;

        CharacterController controller = actor.GetComponent<CharacterController>();
        if (controller == null)
            controller = actor.GetComponentInChildren<CharacterController>();

        bool reEnableController = controller != null && controller.enabled;
        if (reEnableController)
            controller.enabled = false;

        actor.position = position;

        if (faceTowardAnchor)
        {
            Vector3 lookDirection = anchorPosition - actor.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.0001f)
                actor.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        if (reEnableController)
            controller.enabled = true;
    }

    private static Transform FindTransformByNameIncludingInactive(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject active = GameObject.Find(objectName);
        if (active != null)
            return active.transform;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t != null && t.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return t;
        }

        return null;
    }

    private static Transform FindTransformByNameFragmentIncludingInactive(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null)
                continue;

            if (t.name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }

        return null;
    }
}
