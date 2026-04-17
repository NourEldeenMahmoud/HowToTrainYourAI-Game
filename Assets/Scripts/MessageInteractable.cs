using UnityEngine;

/// <summary>
/// Opens message UI on interact; blocks movement and camera look. Tab closes the message (via ControlManager).
/// Interact again (E) while the message is open also closes it.
/// </summary>
public class MessageInteractable : SimpleInteractable
{
    [SerializeField] private GameObject messageUiRoot;
    [Tooltip("Optional. If null, finds a ControlManager in the scene.")]
    [SerializeField] private ControlManager controlManager;

    private bool messageIsOpen;

    private void Awake()
    {
        if (controlManager == null)
        {
            controlManager = FindFirstObjectByType<ControlManager>();
        }
    }

    private void OnDestroy()
    {
        CloseMessageAndUnlockIfNeeded();
    }

    public override void Interact()
    {
        if (messageIsOpen)
        {
            CloseMessageAndUnlockIfNeeded();
            return;
        }

        base.Interact();

        if (messageUiRoot == null)
        {
            return;
        }

        if (!messageUiRoot.scene.IsValid())
        {
            Debug.LogWarning(
                $"{nameof(MessageInteractable)} on '{name}': {nameof(messageUiRoot)} must be the scene instance from the Hierarchy, not a prefab from the Project window.",
                this);
            return;
        }

        if (controlManager == null)
        {
            controlManager = FindFirstObjectByType<ControlManager>();
        }

        if (controlManager == null)
        {
            Debug.LogWarning($"{nameof(MessageInteractable)} on '{name}': no {nameof(ControlManager)} — showing message without gameplay freeze.", this);
            messageUiRoot.SetActive(true);
            return;
        }

        messageIsOpen = true;
        messageUiRoot.SetActive(true);
        controlManager.AddMessageBlockingTabListener(OnTabCloseMessage);
        controlManager.SetMessageBlocksPlayerControls(true);
    }

    private void OnTabCloseMessage()
    {
        CloseMessageAndUnlockIfNeeded();
    }

    private void CloseMessageAndUnlockIfNeeded()
    {
        if (!messageIsOpen)
        {
            return;
        }

        messageIsOpen = false;

        if (messageUiRoot != null && messageUiRoot.scene.IsValid())
        {
            messageUiRoot.SetActive(false);
        }

        if (controlManager != null)
        {
            controlManager.RemoveMessageBlockingTabListener(OnTabCloseMessage);
            controlManager.SetMessageBlocksPlayerControls(false);
        }
    }
}
