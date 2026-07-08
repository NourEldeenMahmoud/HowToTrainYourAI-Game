using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG3DebugHudPlaceholder : MonoBehaviour
{
    [SerializeField] private MiniGame3Manager manager;
    [SerializeField] private TMP_Text tmpStatusText;
    [SerializeField] private Text legacyStatusText;
    [SerializeField] private bool mirrorToConsole = true;

    private void Awake()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<MiniGame3Manager>();
        }
    }

    private void OnEnable()
    {
        if (manager == null) manager = FindFirstObjectByType<MiniGame3Manager>();
        if (manager == null) return;
        manager.PhaseChanged += OnPhaseChanged;
        manager.TaskStarted += OnTaskStarted;
        manager.TaskCompleted += OnTaskCompleted;
        manager.TaskReset += OnTaskReset;
        manager.Feedback += OnFeedback;
        manager.MiniGameCompleted += OnMiniGameCompleted;
    }

    private void OnDisable()
    {
        if (manager == null) return;
        manager.PhaseChanged -= OnPhaseChanged;
        manager.TaskStarted -= OnTaskStarted;
        manager.TaskCompleted -= OnTaskCompleted;
        manager.TaskReset -= OnTaskReset;
        manager.Feedback -= OnFeedback;
        manager.MiniGameCompleted -= OnMiniGameCompleted;
    }

    private void OnPhaseChanged(MiniGame3Phase phase) => SetStatus($"Phase: {phase}");
    private void OnTaskStarted(MG3TaskDefinition task, int index, int total) => SetStatus(task == null ? "Task started" : $"Task {index + 1}/{total}: {task.TaskName}");
    private void OnTaskCompleted(MG3TaskDefinition task, int index, int total) => SetStatus(task == null ? "Task complete" : $"Task complete: {task.TaskName}");
    private void OnTaskReset(MG3TaskDefinition task) => SetStatus(task == null ? "Task reset" : $"Task reset: {task.TaskName}");
    private void OnFeedback(string msg) => SetStatus(msg);
    private void OnMiniGameCompleted() => SetStatus("MiniGame 3 completed");

    private void SetStatus(string message)
    {
        if (tmpStatusText != null) tmpStatusText.text = message;
        if (legacyStatusText != null) legacyStatusText.text = message;
        if (mirrorToConsole) Debug.Log($"[MG3DebugHUD] {message}", this);
    }
}
