using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// MiniGame2: Cinemachine top-down that follows the robot, with manual pan (offset) and zoom.
/// - Follow: camera target stays on the robot each frame (LateUpdate) + optional pan offset on XZ.
/// - Pan: Left mouse drag adds to the offset (relative to robot).
/// - Zoom: intentionally NOT handled here (configure in Cinemachine components instead).
/// </summary>
[DisallowMultipleComponent]
public class MG2CinemachineTopDownInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera cmCamera;
    [Tooltip("Assign the same object used as CinemachineCamera Tracking Target (Follow). Moved by this script.")]
    [SerializeField] private Transform cameraTarget;
    [Tooltip("Robot root to follow. If empty, tries Tag \"Robot\" or TileClickMover.moverTransform.")]
    [SerializeField] private Transform robotTransform;
    [SerializeField] private ControlManager controlManager;

    [Header("Follow")]
    [Tooltip("Vertical offset of the tracking point above the robot (usually 0; height comes from Orbital Follow).")]
    [SerializeField] private float followYOffset;

    [Header("Pan (offset from robot)")]
    [SerializeField, Min(0.0001f)] private float panSpeed = 0.02f;
    [Tooltip("Max distance the pan offset can drift from the robot on XZ (0 = unlimited).")]
    [SerializeField, Min(0f)] private float maxPanOffset = 12f;
    [Tooltip("Default pan offset applied when the scene starts (X = world X, Y = world Z).")]
    [SerializeField] private Vector2 defaultPanOffsetXZ = Vector2.zero;
    [Tooltip("If true, the default pan offset is applied on Start().")]
    [SerializeField] private bool applyDefaultPanOnStart = true;

    private Vector3 panOffsetXZ;
    private bool isPanning;

    private void Awake()
    {
        if (cmCamera == null)
            cmCamera = GetComponent<CinemachineCamera>();

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();
    }

    private void OnEnable()
    {
        ResolveRefs();
        ResolveRobot();
    }

    private void Start()
    {
        if (applyDefaultPanOnStart)
        {
            panOffsetXZ = new Vector3(defaultPanOffsetXZ.x, 0f, defaultPanOffsetXZ.y);
            ClampPanOffset();
        }
    }

    private void ResolveRefs()
    {
        if (cmCamera == null)
            cmCamera = FindFirstObjectByType<CinemachineCamera>();
    }

    private void ResolveRobot()
    {
        if (robotTransform != null) return;

        GameObject go = GameObject.FindGameObjectWithTag("Robot");
        if (go != null)
        {
            robotTransform = go.transform;
            return;
        }

        TileClickMover mover = FindFirstObjectByType<TileClickMover>();
        if (mover != null)
            robotTransform = mover.MoverRoot;
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        if (cameraTarget == null || cmCamera == null) return;

        if (controlManager != null && controlManager.IsInputLocked)
            return;

        // Left-drag = pan camera; right-click = robot movement (TileClickMover).
        if (Mouse.current.leftButton.wasPressedThisFrame) isPanning = true;
        else if (Mouse.current.leftButton.wasReleasedThisFrame) isPanning = false;

        Vector2 delta = Mouse.current.delta.ReadValue();

        if (isPanning && delta != Vector2.zero)
        {
            Transform camT = cmCamera.transform;
            Vector3 right = camT.right;
            Vector3 forward = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
            Vector3 move = (-right * delta.x + -forward * delta.y) * panSpeed;
            panOffsetXZ += new Vector3(move.x, 0f, move.z);
            ClampPanOffset();
        }
    }

    private void LateUpdate()
    {
        if (cameraTarget == null) return;
        if (robotTransform == null)
            ResolveRobot();
        if (robotTransform == null) return;

        Vector3 basePos = robotTransform.position;
        basePos.y += followYOffset;
        cameraTarget.position = basePos + panOffsetXZ;
    }

    /// <summary>Recenter pan so target sits on the robot (optional: bind to a UI button).</summary>
    public void ResetPan()
    {
        panOffsetXZ = Vector3.zero;
    }

    public void SetPanOffsetWorldXZ(Vector2 worldXZ)
    {
        panOffsetXZ = new Vector3(worldXZ.x, 0f, worldXZ.y);
        ClampPanOffset();
    }

    private void ClampPanOffset()
    {
        if (maxPanOffset <= 0f) return;

        Vector3 flat = new Vector3(panOffsetXZ.x, 0f, panOffsetXZ.z);
        if (flat.magnitude > maxPanOffset)
            panOffsetXZ = new Vector3(flat.normalized.x * maxPanOffset, 0f, flat.normalized.z * maxPanOffset);
    }
}
