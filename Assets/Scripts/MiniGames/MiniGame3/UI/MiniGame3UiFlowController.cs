using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MiniGame3UiFlowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame3Manager manager;
    [SerializeField] private MG3PushController pushController;

    [Header("Instruction Canvas")]
    [SerializeField] private GameObject instructionCanvasRoot;
    [SerializeField] private Button beginButton;
    [SerializeField, Min(0f)] private float beginToTaskDelay = 0.35f;

    [Header("Task Canvas")]
    [SerializeField] private GameObject taskCanvasRoot;
    [SerializeField] private GameObject task1Panel;
    [SerializeField] private GameObject task2Panel;
    [SerializeField] private GameObject task3Panel;
    [SerializeField, Min(0f)] private float taskSwitchDelay = 0.35f;

    [Header("Push Prompt")]
    [SerializeField] private GameObject ePressPanel;

    [Header("Floating Feedback")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField, Min(0f)] private float instructionTextDuration = 2f;
    [SerializeField, Min(0f)] private float taskCompletionTextDuration = 3f;
    [SerializeField] private bool showOnlySelectedInstructionMessages = true;
    [SerializeField] private string ambiguousPushMessage = "There are two neighbor devices: you can only push one.";
    [SerializeField] private string deadlockMessage = "This device reached a dead end. Resetting current task...";
    [SerializeField] private string taskCompletedMessage = "Task completed! Preparing next task...";

    [Header("Result Canvas")]
    [SerializeField] private GameObject resultCanvasRoot;
    [SerializeField] private TMP_Text pushesText;
    [SerializeField] private TMP_Text resetsText;
    [SerializeField] private TMP_Text finalGradeText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private string nextSceneName = "";

    private Coroutine feedbackRoutine;
    private int lastKnownTaskIndex = -999;

    private void Awake()
    {
        if (manager == null) manager = FindFirstObjectByType<MiniGame3Manager>();
        if (pushController == null) pushController = FindFirstObjectByType<MG3PushController>();
    }

    private void OnEnable()
    {
        if (manager == null) manager = FindFirstObjectByType<MiniGame3Manager>();

        if (beginButton != null) beginButton.onClick.AddListener(OnBeginClicked);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);

        if (manager != null)
        {
            manager.TaskStarted += OnTaskStarted;
            manager.TaskCompleted += OnTaskCompleted;
            manager.TaskReset += OnTaskReset;
            manager.Feedback += OnFeedback;
            manager.MiniGameCompleted += OnMiniGameCompleted;
            manager.StatsChanged += OnStatsChanged;
        }
    }

    private void OnDisable()
    {
        if (beginButton != null) beginButton.onClick.RemoveListener(OnBeginClicked);
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);

        if (manager != null)
        {
            manager.TaskStarted -= OnTaskStarted;
            manager.TaskCompleted -= OnTaskCompleted;
            manager.TaskReset -= OnTaskReset;
            manager.Feedback -= OnFeedback;
            manager.MiniGameCompleted -= OnMiniGameCompleted;
            manager.StatsChanged -= OnStatsChanged;
        }
    }

    private void Start()
    {
        EnsureUiEventSystem();

        SetTaskPanelsVisible(false, false, false);
        if (taskCanvasRoot != null) taskCanvasRoot.SetActive(false);
        if (resultCanvasRoot != null) resultCanvasRoot.SetActive(false);
        if (instructionCanvasRoot != null) instructionCanvasRoot.SetActive(true);
        if (ePressPanel != null) ePressPanel.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        if (manager != null)
        {
            manager.SetGameplayInputEnabled(false);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        // Keep mouse interaction available whenever instruction/result UI is active.
        bool interactiveUiVisible = (instructionCanvasRoot != null && instructionCanvasRoot.activeInHierarchy)
                                   || (resultCanvasRoot != null && resultCanvasRoot.activeInHierarchy);
        if (interactiveUiVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (ePressPanel == null || pushController == null || manager == null)
        {
            return;
        }

        bool show = manager.CurrentPhase == MiniGame3Phase.WaitingForInput && pushController.HasValidPushCandidate();
        ePressPanel.SetActive(show);

        // Keep task panels in sync with manager state in case events were missed or out-of-order.
        if (taskCanvasRoot != null && taskCanvasRoot.activeInHierarchy && manager != null)
        {
            int idx = manager.CurrentTaskIndex;
            if (idx != lastKnownTaskIndex)
            {
                lastKnownTaskIndex = idx;
                SetTaskPanelsVisible(idx == 0, idx == 1, idx == 2);
            }
        }
    }

    private void OnBeginClicked()
    {
        StartCoroutine(BeginFlow());
    }

    private IEnumerator BeginFlow()
    {
        if (instructionCanvasRoot != null) instructionCanvasRoot.SetActive(false);
        if (beginToTaskDelay > 0f) yield return new WaitForSeconds(beginToTaskDelay);

        if (taskCanvasRoot != null) taskCanvasRoot.SetActive(true);
        SetTaskPanelsVisible(false, false, false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (manager != null)
        {
            manager.SetGameplayInputEnabled(true);
            manager.StartMiniGame();
        }
    }

    private void OnTaskStarted(MG3TaskDefinition task, int index, int total)
    {
        Debug.Log($"[UI] OnTaskStarted index={index} total={total} task={(task==null?"<null>":task.TaskName)}", this);
        // Force Update() to re-sync on the very next frame by invalidating lastKnownTaskIndex.
        lastKnownTaskIndex = -999;
    }

    private void OnTaskCompleted(MG3TaskDefinition task, int index, int total)
    {
        Debug.Log($"[UI] OnTaskCompleted index={index} total={total} task={(task==null?"<null>":task.TaskName)}", this);
        // Hide all task panels — Update() will show the next task's panel once CurrentTaskIndex advances.
        SetTaskPanelsVisible(false, false, false);
        ShowInstructionText(taskCompletedMessage, taskCompletionTextDuration);
    }

    private void OnTaskReset(MG3TaskDefinition task)
    {
        if (manager == null) return;
        int i = manager.CurrentTaskIndex;
        SetTaskPanelsVisible(i == 0, i == 1, i == 2);
    }

    private void OnFeedback(string message)
    {
        if (instructionText == null || string.IsNullOrWhiteSpace(message)) return;

        if (!showOnlySelectedInstructionMessages)
        {
            ShowInstructionText(message, instructionTextDuration);
            return;
        }

        if (message.Contains("Ambiguous push", StringComparison.OrdinalIgnoreCase))
        {
            ShowInstructionText(ambiguousPushMessage, instructionTextDuration);
            return;
        }

        if (message.Contains("Deadlock detected", StringComparison.OrdinalIgnoreCase))
        {
            ShowInstructionText(deadlockMessage, instructionTextDuration);
            return;
        }

        if (message.Contains("Task complete", StringComparison.OrdinalIgnoreCase))
        {
            ShowInstructionText(taskCompletedMessage, taskCompletionTextDuration);
            return;
        }
    }

    private IEnumerator ShowFeedbackForDuration(string message, float duration)
    {
        instructionText.text = message;
        instructionText.gameObject.SetActive(true);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
        instructionText.gameObject.SetActive(false);
    }

    private void ShowInstructionText(string message, float duration = -1f)
    {
        if (instructionText == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        float useDuration = duration >= 0f ? duration : instructionTextDuration;
        feedbackRoutine = StartCoroutine(ShowFeedbackForDuration(message, useDuration));
    }

    private void OnMiniGameCompleted()
    {
        if (resultCanvasRoot != null) resultCanvasRoot.SetActive(true);
        if (taskCanvasRoot != null) taskCanvasRoot.SetActive(false);
        if (ePressPanel != null) ePressPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (manager != null)
        {
            manager.SetGameplayInputEnabled(false);
            OnStatsChanged(manager.TotalPushes, manager.TotalResets);
        }
    }

    private void OnStatsChanged(int pushes, int resets)
    {
        if (pushesText != null) pushesText.text = pushes.ToString();
        if (resetsText != null) resetsText.text = resets.ToString();
        if (finalGradeText != null) finalGradeText.text = CalculateGrade(resets);
    }

    private void OnRetryClicked()
    {
        if (manager != null)
        {
            manager.ReloadCurrentScene();
        }
        else
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }

    private void OnNextClicked()
    {
        SceneTransitionFader.TransitionToScene("PostCredits", -1, 1f);
    }

    private static string CalculateGrade(int resets)
    {
        if (resets <= 0) return "A";
        if (resets <= 2) return "B";
        if (resets <= 4) return "C";
        return "D";
    }

    private void SetTaskPanelsVisible(bool task1, bool task2, bool task3)
    {
        if (task1Panel != null) task1Panel.SetActive(task1);
        if (task2Panel != null) task2Panel.SetActive(task2);
        if (task3Panel != null) task3Panel.SetActive(task3);
    }

    private void EnsureUiEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}
