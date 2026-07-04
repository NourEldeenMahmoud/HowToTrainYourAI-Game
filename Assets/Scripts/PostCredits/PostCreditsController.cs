using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PostCreditsController : MonoBehaviour
{
    [Serializable]
    public class CreditEntry
    {
        public string name = "";
        public string role = "";
    }

    [Header("Credits Data")]
    [SerializeField] private List<CreditEntry> credits = new List<CreditEntry>();

    [Header("Scroll Settings")]
    [SerializeField] private TMP_Text creditsText;
    [SerializeField] private float scrollDuration = 10f;
    [SerializeField] private float pauseBeforeAdvance = 2f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float horizontalPadding = 100f;

    [Header("Advance Target")]
    [SerializeField] private string nextSceneName = "Nour";

    private RectTransform textRect;
    private RectTransform viewportRect;

    private void Start()
    {
        if (creditsText == null) return;

        textRect = creditsText.rectTransform;
        viewportRect = textRect.parent as RectTransform;

        BuildCreditsText();
        StartCoroutine(ScrollAndAdvance());
    }

    private void BuildCreditsText()
    {
        if (credits == null || credits.Count == 0)
        {
            creditsText.text = "Thank You For Playing";
            return;
        }

        string result = "\n\n\n";
        for (int i = 0; i < credits.Count; i++)
        {
            CreditEntry entry = credits[i];
            string entryText = "";
            if (!string.IsNullOrWhiteSpace(entry.name))
                entryText += $"<size=100%>{entry.name}</size>";
            if (!string.IsNullOrWhiteSpace(entry.role))
                entryText += $"\n<size=70%><color=#AAAAAA>{entry.role}</color></size>";

            result += entryText;
            if (i < credits.Count - 1)
                result += "\n\n";
        }
        result += "\n\n\n\n";
        creditsText.text = result;
    }

    private IEnumerator ScrollAndAdvance()
    {
        yield return null;
        yield return null;

        Canvas.ForceUpdateCanvases();

        float textHeight = creditsText.preferredHeight;
        float canvasHeight = viewportRect.rect.height;

        textRect.sizeDelta = new UnityEngine.Vector2(viewportRect.rect.width - horizontalPadding * 2f, textHeight);

        Canvas.ForceUpdateCanvases();

        float startY = -(canvasHeight + 10f);
        float endY = textHeight + 10f;

        textRect.anchoredPosition = new UnityEngine.Vector2(0f, startY);

        float elapsed = 0f;
        while (elapsed < scrollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scrollDuration);
            float newY = Mathf.Lerp(startY, endY, t);
            textRect.anchoredPosition = new UnityEngine.Vector2(0f, newY);
            yield return null;
        }

        textRect.anchoredPosition = new UnityEngine.Vector2(0f, endY);

        yield return new WaitForSeconds(pauseBeforeAdvance);

        GameSessionFlowFlags.RequestSkipMainMenuOnce();
        SceneTransitionFader.TransitionToScene(nextSceneName, -1, fadeDuration);
    }
}
