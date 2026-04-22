using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using TMPro;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ControlManager controlManager;
    [Tooltip("Root of the interact hint in the HUD (e.g. button + TMP children). Shown only when an interactable is in range and aimed at.")]
    [FormerlySerializedAs("interactPromptText")]
    [SerializeField] private GameObject interactActionUi;
    [Tooltip("Optional. If assigned, this specific Tab hint element is hidden when control switching is disabled.")]
    [SerializeField] private GameObject tabActionUi;
    [SerializeField] private GameObject interactPromptUi;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;

    private SimpleInteractable currentInteractable;

    private void Update()
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        ResolveHudReferences();

        DetectInteractable();
        UpdateInteractUi();
    }

    private void ResolveHudReferences()
    {
        if (interactActionUi == null && interactPromptUi != null)
            interactActionUi = interactPromptUi;

        if (interactActionUi == null)
        {
            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || string.IsNullOrEmpty(label.text))
                    continue;

                if (label.text.IndexOf("Press [E]", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    label.text.IndexOf("Interact", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    interactActionUi = label.transform.parent != null ? label.transform.parent.gameObject : label.gameObject;
                    break;
                }
            }
        }

        if (tabActionUi == null)
        {
            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TMP_Text label = labels[i];
                if (label == null || string.IsNullOrEmpty(label.text))
                    continue;

                if (label.text.IndexOf("Enter Control Mode", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    label.text.Trim().Equals("Tab", System.StringComparison.OrdinalIgnoreCase))
                {
                    Transform t = label.transform;
                    if (t.parent != null && t.parent.parent != null)
                        tabActionUi = t.parent.parent.gameObject;
                    else if (t.parent != null)
                        tabActionUi = t.parent.gameObject;
                    else
                        tabActionUi = t.gameObject;
                    break;
                }
            }
        }
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
        bool show = currentInteractable != null;
        if (show && playerCamera != null)
        {
            Vector3 screenPos = playerCamera.WorldToScreenPoint(currentInteractable.PromptWorldPosition);
            show = screenPos.z > 0f;
        }

        if (interactActionUi != null)
            interactActionUi.SetActive(show);

        if (tabActionUi == null && interactActionUi != null)
        {
            Transform tab = interactActionUi.transform.Find("Tab button");
            if (tab != null)
                tabActionUi = tab.gameObject;
            else
            {
                TMPro.TMP_Text[] labels = interactActionUi.GetComponentsInChildren<TMPro.TMP_Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    TMPro.TMP_Text label = labels[i];
                    if (label == null || string.IsNullOrEmpty(label.text))
                        continue;

                    if (label.text.IndexOf("tab", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        tabActionUi = label.transform.parent != null ? label.transform.parent.gameObject : label.gameObject;
                        break;
                    }
                }
            }
        }

        if (tabActionUi != null)
        {
            bool switchEnabled = controlManager == null || controlManager.IsSwitchEnabled;
            tabActionUi.SetActive(switchEnabled);
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;

        currentInteractable.Interact();
    }
}
