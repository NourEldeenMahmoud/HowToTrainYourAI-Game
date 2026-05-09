using UnityEngine;

public class MG3CameraFovLimiter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(1f)] private float minFov = 35f;
    [SerializeField, Min(1f)] private float maxFov = 60f;
    [SerializeField] private bool enforceEveryFrame = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        ApplyClamp();
    }

    private void LateUpdate()
    {
        if (enforceEveryFrame)
        {
            ApplyClamp();
        }
    }

    private void OnValidate()
    {
        if (maxFov < minFov)
        {
            maxFov = minFov;
        }

        ApplyClamp();
    }

    private void ApplyClamp()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView, minFov, maxFov);
    }
}
