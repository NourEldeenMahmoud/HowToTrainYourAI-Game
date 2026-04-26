using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionFader : MonoBehaviour
{
    private static SceneTransitionFader instance;
    private bool isTransitioning;

    private Canvas fadeCanvas;
    private Image fadeImage;

    public static void TransitionToScene(string sceneName, int sceneIndex, float fadeDuration)
    {
        SceneTransitionFader fader = GetOrCreate();
        if (fader.isTransitioning)
            return;

        fader.StartCoroutine(fader.RunTransition(sceneName, sceneIndex, fadeDuration));
    }

    private IEnumerator RunTransition(string sceneName, int sceneIndex, float fadeDuration)
    {
        isTransitioning = true;
        float duration = Mathf.Max(0.01f, fadeDuration);

        SetInputBlocked(true);
        yield return FadeTo(1f, duration);

        AsyncOperation loadOp = !string.IsNullOrWhiteSpace(sceneName)
            ? SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
            : SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single);

        while (loadOp != null && !loadOp.isDone)
            yield return null;

        yield return null;
        yield return FadeTo(0f, duration);
        SetInputBlocked(false);
        isTransitioning = false;
    }

    private static SceneTransitionFader GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<SceneTransitionFader>();
        if (instance != null)
        {
            instance.EnsureOverlay();
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }

        GameObject go = new GameObject("SceneTransitionFader");
        instance = go.AddComponent<SceneTransitionFader>();
        instance.EnsureOverlay();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void EnsureOverlay()
    {
        if (fadeCanvas == null)
        {
            fadeCanvas = gameObject.GetComponent<Canvas>();
            if (fadeCanvas == null)
                fadeCanvas = gameObject.AddComponent<Canvas>();

            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = short.MaxValue;
        }

        if (gameObject.GetComponent<CanvasScaler>() == null)
            gameObject.AddComponent<CanvasScaler>();

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        if (fadeImage == null)
        {
            Transform existing = transform.Find("Fade");
            if (existing != null)
                fadeImage = existing.GetComponent<Image>();

            if (fadeImage == null)
            {
                GameObject imageGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
                imageGo.transform.SetParent(transform, false);
                fadeImage = imageGo.GetComponent<Image>();
            }
        }

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = true;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        EnsureOverlay();

        Color c = fadeImage.color;
        float startAlpha = c.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;
    }

    private void SetInputBlocked(bool blocked)
    {
        if (fadeImage != null)
            fadeImage.raycastTarget = blocked;
    }
}
