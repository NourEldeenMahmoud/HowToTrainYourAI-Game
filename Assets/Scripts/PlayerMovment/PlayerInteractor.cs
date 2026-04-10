using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TMP_Text interactPromptText;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;

    private SimpleInteractable currentInteractable;

    private void Update()
    {
        DetectInteractable();
        UpdatePrompt();
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

    private void UpdatePrompt()
    {
        if (interactPromptText == null || playerCamera == null) return;

        bool canInteract = currentInteractable != null;
        if (!canInteract)
        {
            HidePrompt();
            return;
        }

        Vector3 screenPos = playerCamera.WorldToScreenPoint(currentInteractable.PromptWorldPosition);
        bool isInFront = screenPos.z > 0f;
        interactPromptText.gameObject.SetActive(isInFront);
        if (!isInFront) return;

        interactPromptText.transform.position = screenPos;
        interactPromptText.text = currentInteractable.InteractPrompt;
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;

        currentInteractable.Interact();
    }

    private void HidePrompt()
    {
        interactPromptText.gameObject.SetActive(false);
        interactPromptText.text = string.Empty;
    }
}
