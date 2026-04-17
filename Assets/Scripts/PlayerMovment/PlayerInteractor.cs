using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Root of the interact hint in the HUD (e.g. button + TMP children). Shown only when an interactable is in range and aimed at.")]
    [FormerlySerializedAs("interactPromptText")]
    [SerializeField] private GameObject interactActionUi;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;

    private SimpleInteractable currentInteractable;

    private void Update()
    {
        DetectInteractable();
        UpdateInteractUi();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;

        TryInteract();
    }

    private void DetectInteractable()
    {
        currentInteractable = null;

        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactLayers, QueryTriggerInteraction.Collide);
        if (hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            // Ignore self hits so third-person camera doesn't lock on player collider.
            if (hits[i].collider.transform.root == transform.root) continue;

            currentInteractable = hits[i].collider.GetComponentInParent<SimpleInteractable>();
            if (currentInteractable != null) return;
        }
    }

    private void UpdateInteractUi()
    {
        if (interactActionUi == null) return;

        bool show = currentInteractable != null;
        if (show && playerCamera != null)
        {
            Vector3 screenPos = playerCamera.WorldToScreenPoint(currentInteractable.PromptWorldPosition);
            show = screenPos.z > 0f;
        }

        interactActionUi.SetActive(show);
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;

        currentInteractable.Interact();
    }
}
