using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Optional: for TextMeshPro support

public class SlideUI : MonoBehaviour
{
    public RectTransform uiElement;      // Keep for reference if needed
    public Text uiText;                  // Regular UI Text
    public TMP_Text uiTMPText;           // Or TextMeshPro (assign one)
    public float charsPerSecond = 50f;   // Speed of letter appearance (higher = faster)
    public bool revealOnStart = true;
    public bool triggerWithSpace = false;

    private string fullText;
    private Coroutine revealRoutine;
    private bool initialized;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        if (revealOnStart)
            ShowUI();
    }

    private void OnDisable()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

    }

    void Update()
    {
        // Test trigger (press Space to start revealing)
        if (triggerWithSpace && Input.GetKeyDown(KeyCode.Space))
        {
            ShowUI();
        }
    }

    public void ShowUI()
    {
        InitializeIfNeeded();

        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(RevealText());
    }

    private IEnumerator RevealText()
    {
        // Clear before starting
        if (uiText != null) uiText.text = "";
        else if (uiTMPText != null) uiTMPText.text = "";

        float delay = 1f / charsPerSecond; // time between each letter

        for (int i = 0; i <= fullText.Length; i++)
        {
            string partial = fullText.Substring(0, i);
            if (uiText != null) uiText.text = partial;
            else if (uiTMPText != null) uiTMPText.text = partial;

            yield return new WaitForSeconds(delay);
        }

        revealRoutine = null;
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        if (uiText == null) uiText = GetComponent<Text>();
        if (uiTMPText == null) uiTMPText = GetComponent<TMP_Text>();

        if (uiText != null)
            fullText = uiText.text;
        else if (uiTMPText != null)
            fullText = uiTMPText.text;
        else
            Debug.LogError("No Text or TMP_Text component found on " + gameObject.name);

        initialized = true;
    }
}
