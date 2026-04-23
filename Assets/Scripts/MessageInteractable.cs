using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens message UI on interact; blocks movement and camera look. Tab closes the message (via ControlManager).
/// Interact again (E) while the message is open also closes it.
/// </summary>
public class MessageInteractable : SimpleInteractable
{
    public static bool HasOpenedAnyMessage { get; private set; }
    public static bool IsAnyMessageOpen => openMessageCount > 0;

    private static int openMessageCount;

    public static void ResetRuntimeSessionState()
    {
        HasOpenedAnyMessage = false;
        openMessageCount = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionFlags()
    {
        ResetRuntimeSessionState();
    }

    [SerializeField] private GameObject messageUiRoot;
    [Tooltip("Optional explicit Message Tab Button object under the message canvas.")]
    [SerializeField] private GameObject messageTabButtonUi;
    [Tooltip("Optional. If null, finds a ControlManager in the scene.")]
    [SerializeField] private ControlManager controlManager;

    private bool messageIsOpen;

    private void Awake()
    {
        if (controlManager == null)
        {
            controlManager = FindFirstObjectByType<ControlManager>();
        }

        ResolveMessageTabButtonReference();
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
        HasOpenedAnyMessage = true;
        openMessageCount++;
        messageUiRoot.SetActive(true);
        EnsureMessageTabButtonState(true);
        controlManager.SetSwitchEnabled(true);
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
        if (openMessageCount > 0)
            openMessageCount--;

        if (messageUiRoot != null && messageUiRoot.scene.IsValid())
        {
            messageUiRoot.SetActive(false);
        }

        EnsureMessageTabButtonState(false);

        if (controlManager != null)
        {
            controlManager.RemoveMessageBlockingTabListener(OnTabCloseMessage);
            controlManager.SetMessageBlocksPlayerControls(false);
        }
    }

    private void ResolveMessageTabButtonReference()
    {
        if (messageUiRoot == null || messageTabButtonUi != null)
            return;

        Transform byName = messageUiRoot.transform.Find("Message Tab Button");
        if (byName != null)
        {
            messageTabButtonUi = byName.gameObject;
            return;
        }

        Button[] buttons = messageUiRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (b == null)
                continue;

            string name = b.gameObject.name;
            if (name.IndexOf("tab", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            messageTabButtonUi = b.gameObject;
            return;
        }
    }

    private void EnsureMessageTabButtonState(bool active)
    {
        ResolveMessageTabButtonReference();
        if (messageTabButtonUi == null)
            return;

        messageTabButtonUi.SetActive(active);

        Button button = messageTabButtonUi.GetComponent<Button>();
        if (button != null)
            button.interactable = active;
    }
}
