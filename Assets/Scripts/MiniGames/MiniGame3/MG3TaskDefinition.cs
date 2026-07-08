using UnityEngine;

public class MG3TaskDefinition : MonoBehaviour
{
    [Header("Task")]
    [SerializeField] private string taskName = "Task";
    [SerializeField, TextArea(2, 5)] private string instructions;
    [SerializeField] private MG3TaskType taskType = MG3TaskType.ExactPlacement;

    [Header("Layout")]
    [SerializeField] private MG3PushableDevice[] devices;
    [SerializeField] private MG3TargetSlot[] slots;
    [SerializeField] private Vector2Int robotStartCoordinate;

    [Header("Optional UI")]
    [SerializeField] private string targetLayoutSummary;

    public string TaskName => taskName;
    public string Instructions => instructions;
    public MG3TaskType TaskType => taskType;
    public MG3PushableDevice[] Devices => devices;
    public MG3TargetSlot[] Slots => slots;
    public Vector2Int RobotStartCoordinate => robotStartCoordinate;
    public string TargetLayoutSummary => targetLayoutSummary;

    public void ResetSlotVisuals()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].ResetSolvedVisual();
            }
        }
    }
}
