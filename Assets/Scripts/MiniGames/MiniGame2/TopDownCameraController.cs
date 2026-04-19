using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TopDownCameraController : MonoBehaviour
{
    [Header("Optional Cinemachine Camera")]
    [SerializeField] private CinemachineCamera mg2Camera;
    [SerializeField] private int activeCameraPriority = 10;
    [SerializeField] private int inactiveCameraPriority = 0;

    [Header("Rig")]
    [Tooltip("The pivot that pans around the room. Defaults to this transform.")]
    [SerializeField] private Transform pivot;
    [Tooltip("The actual camera transform (can be the Cinemachine camera's transform). If null, uses this transform.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Rotation (Fixed)")]
    [SerializeField, Range(5f, 85f)] private float defaultPitch = 55f;
    [SerializeField] private float defaultYaw = 0f;

    [Header("Pan")]
    [SerializeField, Min(0.01f)] private float panSpeed = 0.02f;

    [Header("Zoom")]
    [SerializeField, Min(0.01f)] private float zoomSpeed = 2.0f;
    [SerializeField, Min(0.01f)] private float minZoomHeight = 4.0f;
    [SerializeField, Min(0.01f)] private float maxZoomHeight = 18.0f;
    [Tooltip("Camera local offset at max zoom height. Zoom scales Y and Z together to preserve angle.")]
    [SerializeField] private Vector3 cameraLocalOffsetAtMaxZoom = new Vector3(0f, 18f, -18f);

    [Header("Constraints")]
    [SerializeField] private Bounds roomBounds = new Bounds(Vector3.zero, new Vector3(10f, 0f, 10f));

    private float currentZoomHeight;
    private Vector2 lastMousePos;
    private bool isPanning;

    private void Awake()
    {
        if (pivot == null) pivot = transform;
        if (cameraTransform == null) cameraTransform = transform;

        currentZoomHeight = Mathf.Clamp(cameraLocalOffsetAtMaxZoom.y, minZoomHeight, maxZoomHeight);
    }

    private void OnEnable()
    {
        if (mg2Camera != null) mg2Camera.Priority = activeCameraPriority;
    }

    private void OnDisable()
    {
        if (mg2Camera != null) mg2Camera.Priority = inactiveCameraPriority;
    }

    private void Start()
    {
        // Fixed rotation (no orbit input).
        cameraTransform.localRotation = Quaternion.Euler(defaultPitch, defaultYaw, 0f);
        ApplyZoomOffset();
        ClampPivotToBounds();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // Pan: right mouse drag only (no orbit).
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isPanning = true;
            lastMousePos = Mouse.current.position.ReadValue();
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isPanning = false;
        }

        if (isPanning)
        {
            Vector2 now = Mouse.current.position.ReadValue();
            Vector2 delta = now - lastMousePos;
            lastMousePos = now;

            // Screen delta -> world delta on XZ.
            Vector3 right = cameraTransform.right;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 move = (-right * delta.x + -forward * delta.y) * panSpeed;

            pivot.position += new Vector3(move.x, 0f, move.z);
            ClampPivotToBounds();
        }

        // Zoom: scroll wheel only.
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (!Mathf.Approximately(scroll, 0f))
        {
            currentZoomHeight = Mathf.Clamp(currentZoomHeight - scroll * 0.01f * zoomSpeed, minZoomHeight, maxZoomHeight);
            ApplyZoomOffset();
        }
    }

    private void ApplyZoomOffset()
    {
        // Scale Y and Z with height so the camera maintains framing.
        float t = Mathf.InverseLerp(maxZoomHeight, minZoomHeight, currentZoomHeight);
        float scaledY = currentZoomHeight;
        float scaledZ = Mathf.Lerp(cameraLocalOffsetAtMaxZoom.z, cameraLocalOffsetAtMaxZoom.z * (minZoomHeight / maxZoomHeight), t);
        cameraTransform.localPosition = new Vector3(cameraLocalOffsetAtMaxZoom.x, scaledY, scaledZ);
    }

    private void ClampPivotToBounds()
    {
        Vector3 p = pivot.position;
        Vector3 c = roomBounds.center;
        Vector3 e = roomBounds.extents;

        p.x = Mathf.Clamp(p.x, c.x - e.x, c.x + e.x);
        p.z = Mathf.Clamp(p.z, c.z - e.z, c.z + e.z);
        pivot.position = p;
    }
}

