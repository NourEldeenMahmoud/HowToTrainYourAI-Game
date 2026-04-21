using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class MiniGame2ResultScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MiniGame2Manager miniGame2Manager;
    [SerializeField] private ControlManager controlManager;

    [Tooltip("Root of the result screen UI (can be inactive at startup). If null, will use this GameObject.")]
    [SerializeField] private GameObject resultScreenRoot;

    [Header("UI - Texts")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text actualEnergyText;
    [SerializeField] private TMP_Text idealEnergyText;
    [SerializeField] private TMP_Text efficiencyPercentText;

    [Header("UI - Buttons")]
    [SerializeField] private Button applyImprovementsButton;
    [SerializeField] private Button retryButton;

    [Header("UI - Bars (Scrollbars)")]
    [SerializeField] private Scrollbar energyEfficiencyBar;
    [SerializeField] private Scrollbar pathEfficiencyBar;
    [SerializeField] private Scrollbar collisionSafetyBar;

    [Header("Behavior")]
    [SerializeField] private bool lockControlsWhileVisible = true;
    [SerializeField] private bool debugToConsole = true;
    [SerializeField] private bool showCollisionMetric = false;

    private void OnEnable()
    {
        if (resultScreenRoot == null) resultScreenRoot = gameObject;
        if (controlManager == null) controlManager = FindFirstObjectByType<ControlManager>();
        if (miniGame2Manager == null) miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();

        if (miniGame2Manager != null)
            miniGame2Manager.MiniGameCompleted += OnMiniGameCompleted;

        if (applyImprovementsButton != null)
            applyImprovementsButton.onClick.AddListener(OnApplyImprovementsClicked);
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnDisable()
    {
        if (miniGame2Manager != null)
            miniGame2Manager.MiniGameCompleted -= OnMiniGameCompleted;

        if (applyImprovementsButton != null)
            applyImprovementsButton.onClick.RemoveListener(OnApplyImprovementsClicked);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    private void OnMiniGameCompleted(MiniGame2EvaluationResult result)
    {
        EnsureEventSystemExists();

        if (debugToConsole)
        {
            Debug.Log($"[MG2][ResultUI] MiniGameCompleted received. final={result.finalScore:F1} tier={result.tier}", this);
            if (resultScreenRoot == null)
                Debug.LogWarning("[MG2][ResultUI] resultScreenRoot is null. Falling back to this GameObject.", this);
        }

        if (resultScreenRoot != null)
            resultScreenRoot.SetActive(true);

        if (lockControlsWhileVisible && controlManager != null)
            controlManager.SetInputLocked(true);

        ApplyResult(result);

        if (debugToConsole)
            Debug.Log("[MG2][ResultUI] Result values applied to UI fields (where assigned).", this);
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private void ApplyResult(MiniGame2EvaluationResult r)
    {
        if (finalScoreText != null) finalScoreText.text = $"{Mathf.RoundToInt(r.finalScore)}%";
        if (tierText != null) tierText.text = r.tier.ToString();

        if (actualEnergyText != null) actualEnergyText.text = r.actualEnergy.ToString("F2");
        if (idealEnergyText != null) idealEnergyText.text = r.idealEnergy.ToString("F2");

        if (efficiencyPercentText != null)
        {
            float pct = r.actualEnergy > 0f ? Mathf.Clamp((r.idealEnergy / r.actualEnergy) * 100f, 0f, 100f) : 0f;
            efficiencyPercentText.text = $"{Mathf.RoundToInt(pct)}%";
        }

        ApplyBar(energyEfficiencyBar, r.energyEfficiencyScore);
        ApplyBar(pathEfficiencyBar, r.pathEfficiencyScore);
        if (collisionSafetyBar != null)
        {
            collisionSafetyBar.gameObject.SetActive(showCollisionMetric);
            if (showCollisionMetric)
                ApplyBar(collisionSafetyBar, r.collisionSafetyScore);
        }
    }

    private static void ApplyBar(Scrollbar bar, float score0To100)
    {
        if (bar == null) return;
        float t = Mathf.Clamp01(score0To100 / 100f);

        bar.transition = Selectable.Transition.None;
        bar.interactable = false;
        Navigation nav = bar.navigation;
        nav.mode = Navigation.Mode.None;
        bar.navigation = nav;

        Graphic barGraphic = bar.GetComponent<Graphic>();
        if (barGraphic != null) barGraphic.raycastTarget = false;
        if (bar.handleRect != null)
        {
            Graphic handleGraphic = bar.handleRect.GetComponent<Graphic>();
            if (handleGraphic != null) handleGraphic.raycastTarget = false;
            bar.handleRect.sizeDelta = Vector2.zero;
            bar.handleRect.anchoredPosition = Vector2.zero;
        }

        bar.direction = Scrollbar.Direction.LeftToRight;
        bar.size = t;
        bar.value = 0f;
    }

    private void OnApplyImprovementsClicked()
    {
        if (debugToConsole)
            Debug.Log("[MG2][ResultUI] Apply Improvements clicked. Stats were already applied automatically at final evaluation.", this);

        if (resultScreenRoot != null) resultScreenRoot.SetActive(false);
        if (lockControlsWhileVisible && controlManager != null) controlManager.SetInputLocked(false);
    }

    private void OnRetryClicked()
    {
        if (miniGame2Manager != null)
            miniGame2Manager.StartMiniGame();

        if (resultScreenRoot != null) resultScreenRoot.SetActive(false);
        if (lockControlsWhileVisible && controlManager != null) controlManager.SetInputLocked(false);
    }
}
