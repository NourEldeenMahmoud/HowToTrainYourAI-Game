using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class DialogueManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DialogueSequence sequence;

    [Header("Scene Bindings")]
    [SerializeField] private SceneBindings bindings;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button nextButton;

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Fade Settings (Scenes Only)")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField, Min(0.05f)] private float sceneTransitionFadeDuration = 0.35f;

    [Header("Developer Shortcut")]
    [SerializeField] private bool enableDeveloperSkipShortcut = true;
    [SerializeField] private Key developerSkipKey = Key.F8;

    private int currentIndex = 0;
    private Camera currentCamera;
    private GameObject currentCharacter;

    private Coroutine typingCoroutine;

    void Start()
    {
        EnsureEventSystem();
        EnsureDialogueCursor();

        if (sequence == null || sequence.steps.Count == 0)
        {
            Debug.LogWarning("No dialogue sequence assigned!");
            return;
        }

        // ✅ تشغيل أول كاميرا فورًا
        var firstStep = sequence.steps[0];
        Camera firstCam = bindings.GetCamera(firstStep.cameraID);

        if (firstCam != null)
        {
            firstCam.gameObject.SetActive(true);
            currentCamera = firstCam;
        }

        // ✅ بداية المشهد بدون سواد
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }

        ShowStep();
    }

    void Update()
    {
        EnsureDialogueCursor();

        if (Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame ||
             Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            NextStep();
        }

        if (Keyboard.current != null && enableDeveloperSkipShortcut &&
            Keyboard.current[developerSkipKey].wasPressedThisFrame)
        {
            SkipDialogueToSceneTransition();
        }
    }

    private void SkipDialogueToSceneTransition()
    {
        if (sequence == null || sequence.steps == null || sequence.steps.Count == 0)
            return;

        int targetIndex = -1;
        for (int i = currentIndex; i < sequence.steps.Count; i++)
        {
            if (sequence.steps[i].loadNewScene)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0)
            targetIndex = sequence.steps.Count - 1;

        currentIndex = Mathf.Clamp(targetIndex, 0, sequence.steps.Count - 1);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        Debug.Log($"[DialogueManager] Developer skip triggered with key {developerSkipKey}.", this);
        NextStep();
    }

    private static void EnsureDialogueCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

#if ENABLE_INPUT_SYSTEM
        _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#else
        _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    public void NextStep()
    {
        var step = sequence.steps[currentIndex];

        // لو النص لسه بيتكتب → كمّله
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = step.dialogueText;
            typingCoroutine = null;
            return;
        }

        // 🎬 Fade + Scene Load فقط
        if (step.loadNewScene)
        {
            if (nextButton != null)
                nextButton.interactable = false;

            if (step.skipMainMenuOnLoad)
                GameSessionFlowFlags.RequestSkipMainMenuOnce();

            LoadSceneWithFade(step.targetSceneIndex, step.targetSceneName);
            return;
        }

        currentIndex++;

        if (currentIndex >= sequence.steps.Count)
        {
            if (nextButton != null)
                nextButton.interactable = false;
            return;
        }

        ShowStep();
    }

    void ShowStep()
    {
        var step = sequence.steps[currentIndex];

        if (nameText != null)
            nameText.text = step.characterName;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(step.dialogueText));

        // ✅ تغيير الكاميرا فورًا بدون Fade
        Camera cam = bindings.GetCamera(step.cameraID);
        if (cam != null && cam != currentCamera)
        {
            if (currentCamera != null)
                currentCamera.gameObject.SetActive(false);

            cam.gameObject.SetActive(true);
            currentCamera = cam;
        }

        // الشخصيات
        GameObject character = bindings.GetCharacter(step.characterID);
        if (character != null)
        {
            if (currentCharacter != null)
                currentCharacter.SetActive(false);

            character.SetActive(true);
            currentCharacter = character;
        }

        if (nextButton != null)
            nextButton.interactable = true;
    }

    IEnumerator TypeText(string text)
    {
        dialogueText.text = "";

        foreach (char letter in text)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
    }

    // 🎬 Fade للسين فقط
    void LoadSceneWithFade(int sceneIndex, string sceneName)
    {
        float duration = sceneTransitionFadeDuration;
        if (duration <= 0f)
            duration = fadeSpeed > 0f ? (1f / fadeSpeed) : 0.35f;

        SceneTransitionFader.TransitionToScene(sceneName, sceneIndex, duration);
    }

    IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        float t = 0;
        Color c = fadeImage.color;

        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            c.a = Mathf.Lerp(0, 1, t);
            fadeImage.color = c;
            yield return null;
        }
    }
}
