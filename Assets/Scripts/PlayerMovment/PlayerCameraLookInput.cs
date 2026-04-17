using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives CinemachineOrbitalFollow horizontal and vertical axes directly from mouse delta
/// for the player camera. Queries ControlManager each frame — no event-timing issues.
/// </summary>
[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class PlayerCameraLookInput : MonoBehaviour
{
    [SerializeField, Min(0f)] private float horizontalSensitivity = 0.15f;
    [SerializeField, Min(0f)] private float verticalSensitivity   = 0.10f;

    private CinemachineOrbitalFollow orbitalFollow;
    private ControlManager controlManager;

    private void Awake()
    {
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
    }

    private void Start()
    {
        controlManager = FindFirstObjectByType<ControlManager>();
    }

    private void Update()
    {
        if (orbitalFollow == null) return;
        if (controlManager == null) return;
        if (!controlManager.IsPlayerControlActive) return;
        if (controlManager.IsPlayerLookSuppressed) return;

        // Don't orbit when cursor is visible (full input lock).
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue();
        if (delta == Vector2.zero) return;

        orbitalFollow.HorizontalAxis.Value += delta.x * horizontalSensitivity;

        orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
            orbitalFollow.VerticalAxis.Value - delta.y * verticalSensitivity,
            orbitalFollow.VerticalAxis.Range.x,
            orbitalFollow.VerticalAxis.Range.y
        );
    }
}
