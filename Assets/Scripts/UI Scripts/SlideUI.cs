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

    private string fullText;
    private bool isRevealing = false;

    void Start()
    {
        // Determine which text component to use
        if (uiText == null) uiText = GetComponent<Text>();
        if (uiTMPText == null) uiTMPText = GetComponent<TMP_Text>();

        // Store the full text and clear it initially
        if (uiText != null)
        {
            fullText = uiText.text;
            uiText.text = "";
        }
        else if (uiTMPText != null)
        {
            fullText = uiTMPText.text;
            uiTMPText.text = "";
        }
        else
        {
            Debug.LogError("No Text or TMP_Text component found on " + gameObject.name);
        }
    }

    void Update()
    {
        // Test trigger (press Space to start revealing)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowUI();
        }
    }

    public void ShowUI()
    {
        if (!isRevealing)
        {
            StartCoroutine(RevealText());
        }
    }

    private IEnumerator RevealText()
    {
        isRevealing = true;

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

        isRevealing = false;
    }
}