using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MG1InstructionSequenceController : MonoBehaviour
{
    public enum InstructionStage
    {
        ReadTableMessage,
        CorrectRobotMovement,
        ReadCorruptedMessage,
        HeadToStorage
    }

    public static MG1InstructionSequenceController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private TMP_Text[] explicitPlayerInstructionTexts;
    [SerializeField] private TMP_Text[] explicitRobotInstructionTexts;

    [Header("Instruction Text")]
    [SerializeField] private string playerReadTableMessageText = "Read the message on the table.";
    [SerializeField] private string robotReadTableMessageText = "";
    [SerializeField] private string playerCorrectMovementText = "Use the robot to correct the movement issues.";
    [SerializeField] private string robotCorrectMovementText = "Correct the movement issues.";
    [SerializeField] private string playerReadCorruptedMessageText = "Read the new message.";
    [SerializeField] private string robotReadCorruptedMessageText = "Read the new message.";
    [SerializeField] private string playerHeadToStorageText = "Head to the storage room.";
    [SerializeField] private string robotHeadToStorageText = "Head to the storage room.";

    [Header("Behavior")]
    [SerializeField] private InstructionStage initialStage = InstructionStage.ReadTableMessage;
    [SerializeField] private bool applyInitialStageOnStart = true;
    [SerializeField] private bool enforceCurrentStage = true;

    [Header("Debug")]
    [SerializeField] private bool enableLogs;

    private InstructionStage currentStage;
    private bool hasStage;

    public InstructionStage CurrentStage => currentStage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceAfterSceneLoad()
    {
        if (FindFirstObjectByType<MiniGame1Manager>() == null)
            return;

        if (FindFirstObjectByType<MG1InstructionSequenceController>() != null)
            return;

        GameObject go = new GameObject("MG1_InstructionSequenceController");
        go.AddComponent<MG1InstructionSequenceController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        if (applyInitialStageOnStart)
            SetStage(initialStage);
    }

    private void OnEnable()
    {
        MessageInteractable.AnyMessageClosed += OnAnyMessageClosed;
    }

    private void OnDisable()
    {
        MessageInteractable.AnyMessageClosed -= OnAnyMessageClosed;
    }

    private void LateUpdate()
    {
        if (enforceCurrentStage && hasStage)
            ApplyCurrentStage();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetStage(InstructionStage stage)
    {
        currentStage = stage;
        hasStage = true;
        ApplyCurrentStage();
        Log("Instruction stage -> " + stage);
    }

    public void SetReadTableMessageStage()
    {
        SetStage(InstructionStage.ReadTableMessage);
    }

    public void SetCorrectRobotMovementStage()
    {
        SetStage(InstructionStage.CorrectRobotMovement);
    }

    public void SetHeadToStorageStage()
    {
        SetStage(InstructionStage.HeadToStorage);
    }

    public void SetReadCorruptedMessageStage()
    {
        SetStage(InstructionStage.ReadCorruptedMessage);
    }

    private void OnAnyMessageClosed(MessageInteractable message)
    {
        if (hasStage && currentStage == InstructionStage.ReadTableMessage)
            SetCorrectRobotMovementStage();
    }

    private void ApplyCurrentStage()
    {
        ResolveReferences();

        string playerText = string.Empty;
        string robotText = string.Empty;

        switch (currentStage)
        {
            case InstructionStage.ReadTableMessage:
                playerText = playerReadTableMessageText;
                robotText = robotReadTableMessageText;
                break;

            case InstructionStage.CorrectRobotMovement:
                playerText = playerCorrectMovementText;
                robotText = robotCorrectMovementText;
                break;

            case InstructionStage.ReadCorruptedMessage:
                playerText = playerReadCorruptedMessageText;
                robotText = robotReadCorruptedMessageText;
                break;

            case InstructionStage.HeadToStorage:
                playerText = playerHeadToStorageText;
                robotText = robotHeadToStorageText;
                break;
        }

        ApplyTextToExplicitList(explicitPlayerInstructionTexts, playerText);
        ApplyTextToExplicitList(explicitRobotInstructionTexts, robotText);

        if (controlManager != null)
        {
            ApplyTextToInstructionTargets(controlManager.PlayerUiRoot, playerText, "Player Instruction Text");
            ApplyTextToInstructionTargets(controlManager.RobotUiRoot, robotText, "Robot Instruction Text");
        }
        else
        {
            ApplyTextToInstructionTargets(GameObject.Find("Player HUD Canvas"), playerText, "Player Instruction Text");
            ApplyTextToInstructionTargets(GameObject.Find("Robot POV Canvas"), robotText, "Robot Instruction Text");
        }
    }

    private void ResolveReferences()
    {
        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();
    }

    private static void ApplyTextToExplicitList(TMP_Text[] texts, string value)
    {
        if (texts == null)
            return;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text tmp = texts[i];
            if (tmp != null)
                tmp.text = value;
        }
    }

    private static void ApplyTextToInstructionTargets(GameObject root, string value, string preferredObjectName)
    {
        if (root == null)
            return;

        TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
        bool appliedPreferred = false;

        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (tmp == null)
                continue;

            if (!tmp.gameObject.name.Equals(preferredObjectName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            tmp.text = value;
            appliedPreferred = true;
        }

        if (appliedPreferred)
            return;

        for (int i = 0; i < all.Length; i++)
        {
            TMP_Text tmp = all[i];
            if (!CanOverwriteInstructionText(tmp))
                continue;

            tmp.text = value;
        }
    }

    private static bool CanOverwriteInstructionText(TMP_Text tmp)
    {
        if (tmp == null)
            return false;

        if (tmp.GetComponentInParent<Button>(true) != null)
            return false;

        if (IsUnderNamedAncestor(tmp.transform, "Message Panel button"))
            return false;

        if (IsInteractionPromptText(tmp))
            return false;

        string n = tmp.gameObject.name;
        bool instructionLike = n.StartsWith("Instruction Text", System.StringComparison.OrdinalIgnoreCase);
        bool objectiveLike = n.StartsWith("Objective Text", System.StringComparison.OrdinalIgnoreCase);
        bool knownObjectiveText = !string.IsNullOrEmpty(tmp.text) &&
            (tmp.text.IndexOf("Reach The Target", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Read the message", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Read the new message", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Correct the movement", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Head to the storage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Free Move", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Drift", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Speed", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             tmp.text.IndexOf("Done", System.StringComparison.OrdinalIgnoreCase) >= 0);

        return instructionLike || objectiveLike || knownObjectiveText;
    }

    private static bool IsInteractionPromptText(TMP_Text tmp)
    {
        if (tmp == null || string.IsNullOrWhiteSpace(tmp.text))
            return false;

        string text = tmp.text;
        return text.IndexOf("Press", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
               (text.IndexOf("[E]", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf(" E", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsUnderNamedAncestor(Transform child, string ancestorName)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.name.Equals(ancestorName, System.StringComparison.OrdinalIgnoreCase))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void Log(string message)
    {
        if (enableLogs)
            Debug.Log("[MG1Instruction] " + message, this);
    }
}
