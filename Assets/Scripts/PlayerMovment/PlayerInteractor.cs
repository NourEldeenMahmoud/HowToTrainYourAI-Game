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
    [SerializeField] private bool autoResolveInteractActionUi;
    [SerializeField] private bool requireMessageOpenedForTab = true;
    [SerializeField] private bool enforceGlobalTabHintHide = true;
    [SerializeField] private bool inferTabActionUi = false;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayers = ~0;

    private SimpleInteractable currentInteractable;
    private bool allowTabThisFrame;
    private bool tabReferenceResolved;

    private void Update()
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        ResolveHudReferences();
        ResolveTabReference();

        DetectInteractable();
        UpdateInteractUi();
    }

    private void LateUpdate()
    {
        if (!enforceGlobalTabHintHide)
            return;

        ForceHideOtherTabHints(allowTabThisFrame);
    }

    private void ResolveHudReferences()
    {
        if (interactActionUi == null && interactPromptUi != null)
            interactActionUi = interactPromptUi;

        if (autoResolveInteractActionUi && interactActionUi == null)
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

        bool switchEnabled = controlManager == null || controlManager.IsSwitchEnabled;
        bool messageOpened = !requireMessageOpenedForTab || MessageInteractable.HasOpenedAnyMessage;
        bool isMessageTarget = currentInteractable is MessageInteractable;
        bool anyMessageOpen = MessageInteractable.IsAnyMessageOpen;
        bool tabContextValid = (show && isMessageTarget) || anyMessageOpen;
        allowTabThisFrame = switchEnabled && messageOpened && tabContextValid;

        if (interactActionUi != null)
        {
            interactActionUi.SetActive(show);
            if (show)
                EnsureInteractEHintVisible(interactActionUi);
        }

        if (inferTabActionUi && tabActionUi == null && interactActionUi != null)
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
            tabActionUi.SetActive(allowTabThisFrame);
            return;
        }

        if (interactActionUi != null)
        {
            SetTabLikeChildrenActive(interactActionUi, allowTabThisFrame);
        }
    }

    private void ResolveTabReference()
    {
        if (tabReferenceResolved)
            return;

        if (tabActionUi == null)
        {
            tabReferenceResolved = true;
            return;
        }

        GameObject refined = FindBestTabHintObject(tabActionUi);
        if (refined != null)
            tabActionUi = refined;

        tabReferenceResolved = true;
    }

    private static GameObject FindBestTabHintObject(GameObject root)
    {
        if (root == null)
            return null;

        if (root.name.Equals("Tab button", System.StringComparison.OrdinalIgnoreCase))
            return root;

        Transform direct = root.transform.Find("Tab button");
        if (direct != null)
            return direct.gameObject;

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || string.IsNullOrWhiteSpace(label.text))
                continue;

            string text = label.text.Trim();
            bool exactTab = text.Equals("tab", System.StringComparison.OrdinalIgnoreCase);
            bool tabHint = text.IndexOf("tab", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                           text.IndexOf("control mode", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!exactTab && !tabHint)
                continue;

            if (label.transform.parent != null)
                return label.transform.parent.gameObject;

            return label.gameObject;
        }

        return root;
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;

        currentInteractable.Interact();
    }

    private static void SetTabLikeChildrenActive(GameObject root, bool active)
    {
        if (root == null)
            return;

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || string.IsNullOrEmpty(label.text))
                continue;

            string text = label.text;
            if (text.IndexOf("tab", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("control mode", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            GameObject target = GetTabHintToggleTarget(root, label.transform);
            target.SetActive(active);
        }
    }

    private void ForceHideOtherTabHints(bool allowTab)
    {
        if (interactActionUi == null)
            return;

        TMP_Text[] labels = interactActionUi.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || string.IsNullOrEmpty(label.text))
                continue;

            string text = label.text;
            if (text.IndexOf("tab", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("control mode", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (tabActionUi != null && label.transform.IsChildOf(tabActionUi.transform))
                continue;

            GameObject target = GetTabHintToggleTarget(interactActionUi, label.transform);
            if (!allowTab)
                target.SetActive(false);
        }
    }

    private static GameObject GetTabHintToggleTarget(GameObject root, Transform labelTransform)
    {
        if (labelTransform == null)
            return root;

        Transform current = labelTransform;
        while (current.parent != null && current.parent.gameObject != root)
            current = current.parent;

        return current.gameObject == root ? labelTransform.gameObject : current.gameObject;
    }

    private static void EnsureInteractEHintVisible(GameObject root)
    {
        if (root == null)
            return;

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || string.IsNullOrWhiteSpace(label.text))
                continue;

            string text = label.text;
            bool isInteractLabel = text.IndexOf("press [e]", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  text.IndexOf("interact", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isInteractLabel)
                continue;

            Transform current = label.transform;
            while (current != null && current.gameObject != root)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);
                current = current.parent;
            }
        }
    }
}
