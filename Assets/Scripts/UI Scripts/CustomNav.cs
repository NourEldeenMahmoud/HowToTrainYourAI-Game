using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CustomNav : MonoBehaviour
{
    [Header("Navigation")]
    public GameObject defaultButton;

    [Header("Start Menu")]
    [SerializeField] private bool configureAsStartMenu = true;
    [SerializeField] private bool openOnStart = true;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform playerStartOrigin;
    [SerializeField] private Button playButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Scrollbar audioScrollbar;
    [SerializeField, Range(0.02f, 1f)] private float audioHandleSize = 0.2f;
    [SerializeField] private ControlManager controlManager;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GraphicRaycaster menuRaycaster;
    [SerializeField] private GameObject mainMenuBackground;
    [SerializeField] private bool showBackgroundOnlyOnInitialMenu = true;
    [SerializeField] private bool playStartsDialogueSequence = true;
    [SerializeField] private string firstDialogueSceneName = "Office Dialogue Scene";
    [SerializeField, Min(0.05f)] private float sceneTransitionFadeDuration = 0.35f;

    private const string MasterVolumeKey = "MasterVolume";
    private bool menuIsOpen;
    private bool menuInputForced;
    private bool hasLeftInitialMenu;

    private void Awake()
    {
        if (menuRoot == null)
        {
            menuRoot = gameObject;
        }

        if (!configureAsStartMenu)
        {
            return;
        }

        if (menuCanvas == null && menuRoot != null)
            menuCanvas = menuRoot.GetComponent<Canvas>();

        if (menuRaycaster == null && menuRoot != null)
            menuRaycaster = menuRoot.GetComponent<GraphicRaycaster>();

        ResolveStartMenuReferences();

        LoadSavedVolume();

        if (controlManager == null)
        {
            controlManager = FindFirstObjectByType<ControlManager>();
        }
    }

    private void OnEnable()
    {
        if (configureAsStartMenu)
        {
            BindButtons();
            BindAudio();
        }
    }

    private void OnDisable()
    {
        if (configureAsStartMenu)
        {
            UnbindButtons();
            UnbindAudio();
            SetMenuInputState(false);
        }
    }

    private void Start()
    {
        if (defaultButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(defaultButton);

        if (configureAsStartMenu && settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            SetMainMenuButtonsVisible(true);
        }

        if (configureAsStartMenu)
        {
            bool skipMainMenuOnce = GameSessionFlowFlags.ConsumeSkipMainMenuOnce();
            hasLeftInitialMenu = skipMainMenuOnce || !openOnStart;
            SetMenuVisible(skipMainMenuOnce ? false : openOnStart);

            if (skipMainMenuOnce)
                StartCoroutine(EnforceGameplayUiAfterDialogueReturn());
        }

    }

    private void LateUpdate()
    {
        if (!configureAsStartMenu)
            return;

        bool menuVisible = menuIsOpen;
        bool settingsOpen = settingsPanel != null && settingsPanel.activeInHierarchy;
        bool shouldKeepMenuInput = menuInputForced || settingsOpen || menuVisible;

        if (!shouldKeepMenuInput)
            return;

        if (controlManager != null)
        {
            if (!controlManager.IsInputLocked)
                controlManager.SetInputLocked(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        if (configureAsStartMenu && settingsPanel != null && settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SetSettingsOpen(false);
            return;
        }

        if (configureAsStartMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenuVisible(!menuIsOpen);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current != null)
            {
                Button btn = current.GetComponent<Button>();
                if (btn != null && btn.interactable)
                    btn.onClick.Invoke();
            }
        }
    }

    private void ResolveStartMenuReferences()
    {
        Transform root = menuRoot != null ? menuRoot.transform : transform;

        if (playButton == null)
            playButton = FindButtonWithLabel(root, "Play");

        if (newGameButton == null)
            newGameButton = FindButtonWithLabel(root, "New Game");

        if (quitButton == null)
            quitButton = FindButtonWithLabel(root, "Quit");

        if (settingsButton == null)
            settingsButton = FindButtonWithLabel(root, "Settings");

        if (settingsPanel == null)
            settingsPanel = FindGameObjectByName(root, "Settings Image");

        if (mainMenuBackground == null)
            mainMenuBackground = FindGameObjectByName(root, "Background");

        if (playerRoot == null)
        {
            GameObject playerGo = FindGameObjectByName(root, "Player");
            if (playerGo == null)
                playerGo = GameObject.Find("Player");

            if (playerGo != null)
                playerRoot = playerGo.transform;
        }

        if (playerStartOrigin == null)
        {
            GameObject originGo = FindGameObjectByName(root, "Player Start Origin")
                               ?? FindGameObjectByName(root, "PlayerStartOrigin")
                               ?? FindGameObjectByName(root, "Start Origin")
                               ?? GameObject.Find("Player Start Origin")
                               ?? GameObject.Find("PlayerStartOrigin")
                               ?? GameObject.Find("Start Origin");

            if (originGo != null)
                playerStartOrigin = originGo.transform;
        }

        if (audioScrollbar == null && settingsPanel != null)
            audioScrollbar = settingsPanel.GetComponentInChildren<Scrollbar>(true);

        if (settingsCloseButton == null && settingsPanel != null)
            settingsCloseButton = FindButtonWithLabel(settingsPanel.transform, "x")
                               ?? FindButtonWithLabel(settingsPanel.transform, "X")
                               ?? FindButtonWithLabel(settingsPanel.transform, "×")
                               ?? FindButtonByName(settingsPanel.transform, "Button (4)")
                               ?? FindTopRightButton(settingsPanel.transform);

        if (settingsCloseButton != null)
            settingsCloseButton.gameObject.SetActive(true);

        if (settingsCloseButton != null && !settingsCloseButton.enabled)
            settingsCloseButton.enabled = true;

        if (audioScrollbar != null)
            audioHandleSize = Mathf.Clamp(audioScrollbar.size, 0.02f, 1f);

        if (settingsButton == null)
            Debug.LogWarning("[CustomNav] Could not auto-find Settings button.", this);

        if (settingsPanel == null)
            Debug.LogWarning("[CustomNav] Could not auto-find Settings Image panel.", this);

        if (audioScrollbar == null)
            Debug.LogWarning("[CustomNav] Could not auto-find audio scrollbar in Settings Image.", this);

        if (settingsCloseButton == null)
            Debug.LogWarning("[CustomNav] Could not auto-find settings close button.", this);
    }

    private void BindButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(OnSettingsCloseClicked);
    }

    private void UnbindButtons()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(OnNewGameClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.RemoveListener(OnSettingsCloseClicked);
    }

    private void OnPlayClicked()
    {
        bool shouldStartIntroDialogue = !hasLeftInitialMenu;
        if (shouldStartIntroDialogue && playStartsDialogueSequence && !string.IsNullOrWhiteSpace(firstDialogueSceneName))
        {
            hasLeftInitialMenu = true;
            SetSettingsOpen(false);
            SetMenuInputState(true);

            GameSessionFlowFlags.RequestSkipMainMenuOnce();
            SceneTransitionFader.TransitionToScene(firstDialogueSceneName, -1, sceneTransitionFadeDuration);
            return;
        }

        hasLeftInitialMenu = true;
        menuInputForced = false;
        SetSettingsOpen(false);
        SetMenuVisible(false);
    }

    private void OnNewGameClicked()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel == null)
        {
            return;
        }

        SetSettingsOpen(!settingsPanel.activeSelf);
    }

    private void OnSettingsCloseClicked()
    {
        SetSettingsOpen(false);
    }

    public void SetMenuVisible(bool visible)
    {
        menuIsOpen = visible;

        RefreshBackgroundVisibility(visible);

        if (menuCanvas != null)
            menuCanvas.enabled = visible;

        if (menuRaycaster != null)
            menuRaycaster.enabled = visible;

        if (menuCanvas == null && menuRaycaster == null && menuRoot != null)
        {
            menuRoot.SetActive(visible);
        }

        if (!visible && settingsPanel != null)
            settingsPanel.SetActive(false);

        SetMainMenuButtonsVisible(visible);

        if (visible && defaultButton != null)
            EventSystem.current?.SetSelectedGameObject(defaultButton);

        SetMenuInputState(visible);
    }

    private void RefreshBackgroundVisibility(bool menuVisible)
    {
        if (mainMenuBackground == null)
            return;

        bool showBackground = menuVisible;
        if (showBackgroundOnlyOnInitialMenu && hasLeftInitialMenu)
            showBackground = false;

        if (mainMenuBackground.activeSelf != showBackground)
            mainMenuBackground.SetActive(showBackground);
    }

    private void SetMenuInputState(bool menuVisible)
    {
        menuInputForced = menuVisible;
        Time.timeScale = 1f;

        if (controlManager != null)
        {
            controlManager.SetInputLocked(menuVisible);
            return;
        }

        Cursor.lockState = menuVisible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuVisible;
    }

    private IEnumerator EnforceGameplayUiAfterDialogueReturn()
    {
        yield return null;

        if (controlManager == null)
            controlManager = FindFirstObjectByType<ControlManager>();

        controlManager?.ForcePlayerControlState();

        HideByNameIfFound("Robot POV Canvas");
        HideByNameIfFound("Game 2 Robot POV Canvas");
        HideByNameIfFound("Robot Interaction");
        HideByNameIfFound("Robot Interaction Canvas");
    }

    private static void HideByNameIfFound(string exactName)
    {
        if (string.IsNullOrWhiteSpace(exactName))
            return;

        GameObject go = GameObject.Find(exactName);
        if (go != null && go.activeSelf)
            go.SetActive(false);
    }

    private static Button FindButtonWithLabel(Transform root, string label)
    {
        if (root == null || string.IsNullOrWhiteSpace(label))
            return null;

        TMP_Text[] allTexts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text == null)
                continue;

            if (!string.Equals(text.text.Trim(), label, System.StringComparison.OrdinalIgnoreCase))
                continue;

            Button b = text.GetComponentInParent<Button>(true);
            if (b != null)
                return b;
        }

        return null;
    }

    private static GameObject FindGameObjectByName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrWhiteSpace(exactName))
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && t.name == exactName)
                return t.gameObject;
        }

        return null;
    }

    private void BindAudio()
    {
        if (audioScrollbar == null)
            return;

        ApplyAudioScrollbarVisual(AudioListener.volume);
        audioScrollbar.onValueChanged.AddListener(OnAudioScrollbarChanged);
    }

    private void UnbindAudio()
    {
        if (audioScrollbar == null)
            return;

        audioScrollbar.onValueChanged.RemoveListener(OnAudioScrollbarChanged);
    }

    private void LoadSavedVolume()
    {
        float saved = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        AudioListener.volume = Mathf.Clamp01(saved);

        ApplyAudioScrollbarVisual(AudioListener.volume);
    }

    private void OnAudioScrollbarChanged(float value)
    {
        float clamped = Mathf.Clamp01(value);
        AudioListener.volume = clamped;
        PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
        PlayerPrefs.Save();

        ApplyAudioScrollbarVisual(clamped);
    }

    private void ApplyAudioScrollbarVisual(float normalizedValue)
    {
        if (audioScrollbar == null)
            return;

        float t = Mathf.Clamp01(normalizedValue);

        audioScrollbar.direction = Scrollbar.Direction.LeftToRight;
        audioScrollbar.size = Mathf.Clamp(audioHandleSize, 0.02f, 1f);
        audioScrollbar.SetValueWithoutNotify(t);
    }

    private static Button FindTopRightButton(Transform root)
    {
        if (root == null)
            return null;

        Button[] allButtons = root.GetComponentsInChildren<Button>(true);
        Button best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < allButtons.Length; i++)
        {
            Button b = allButtons[i];
            if (b == null)
                continue;

            RectTransform rt = b.GetComponent<RectTransform>();
            if (rt == null)
                continue;

            float score = rt.anchoredPosition.x + rt.anchoredPosition.y;
            if (score > bestScore)
            {
                bestScore = score;
                best = b;
            }
        }

        return best;
    }

    private static Button FindButtonByName(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrWhiteSpace(exactName))
            return null;

        Button[] allButtons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button b = allButtons[i];
            if (b != null && b.gameObject.name == exactName)
                return b;
        }

        return null;
    }

    private void RespawnPlayerAtStart()
    {
        if (playerRoot == null || playerStartOrigin == null)
        {
            Debug.LogWarning("[CustomNav] New Game respawn skipped: assign Player Root and Player Start Origin.", this);
            return;
        }

        CharacterController cc = playerRoot.GetComponent<CharacterController>();
        if (cc == null)
            cc = playerRoot.GetComponentInChildren<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        playerRoot.position = playerStartOrigin.position;
        playerRoot.rotation = playerStartOrigin.rotation;

        if (cc != null)
            cc.enabled = true;
    }

    private void SetMainMenuButtonsVisible(bool visible)
    {
        if (playButton != null)
            playButton.gameObject.SetActive(visible);

        if (newGameButton != null)
            newGameButton.gameObject.SetActive(visible);

        if (quitButton != null)
            quitButton.gameObject.SetActive(visible);

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(visible);
    }

    private void SetSettingsOpen(bool open)
    {
        if (open && !menuIsOpen)
            SetMenuVisible(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(open);

        SetMainMenuButtonsVisible(menuIsOpen && !open);

        if (open && audioScrollbar != null)
            EventSystem.current?.SetSelectedGameObject(audioScrollbar.gameObject);
        else if (!open && defaultButton != null)
            EventSystem.current?.SetSelectedGameObject(defaultButton);

        SetMenuInputState(true);
    }
}
