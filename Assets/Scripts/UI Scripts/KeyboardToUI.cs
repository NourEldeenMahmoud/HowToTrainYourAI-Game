using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Links keyboard keys to UI buttons, simulates visual press effect and executes onClick events.
/// Supports normal press (key down) and hold (key stays pressed while held).
/// </summary>
public class KeyboardToUI : MonoBehaviour
{
    public enum PressType
    {
        Normal, // Trigger once on key down, visual effect fades automatically
        Hold    // Stay pressed while key is held, visual effect persists, onClick triggered once on press start
    }

    [System.Serializable]
    public class KeyButtonMapping
    {
        public KeyCode keyCode;
        public Button targetButton;
        public PressType pressType = PressType.Normal;
        public bool invokeOnClick = true;
    }

    [System.Serializable]
    public class ComplexKeyButtonMapping
    {
        public KeyCode modifierKey;
        public KeyCode mainKey;
        public Button targetButton;
        public PressType pressType = PressType.Normal;
        public bool invokeOnClick = true;
    }

    [Header("Simple Key Mappings")]
    public List<KeyButtonMapping> simpleMappings = new List<KeyButtonMapping>();

    [Header("Complex Key Mappings (e.g. Shift+E)")]
    public List<ComplexKeyButtonMapping> complexMappings = new List<ComplexKeyButtonMapping>();

    [Header("Visual Settings")]
    public float pressDuration = 0.15f; // Used only for Normal press type
    public Color pressColor = Color.gray;

    // Store original colors for each button
    private Dictionary<Button, Color> originalColors = new Dictionary<Button, Color>();

    // Track pressed state for Hold buttons (to avoid multiple onClick invocations)
    private HashSet<Button> heldButtons = new HashSet<Button>();

    void Start()
    {
        // Store original colors for all referenced buttons
        foreach (var mapping in simpleMappings)
            if (mapping.targetButton != null && !originalColors.ContainsKey(mapping.targetButton))
                StoreOriginalColor(mapping.targetButton);

        foreach (var mapping in complexMappings)
            if (mapping.targetButton != null && !originalColors.ContainsKey(mapping.targetButton))
                StoreOriginalColor(mapping.targetButton);
    }

    void StoreOriginalColor(Button button)
    {
        var graphic = button.targetGraphic;
        if (graphic != null)
            originalColors[button] = graphic.color;
        else
            originalColors[button] = Color.white;
    }

    void Update()
    {
        // Process simple mappings
        foreach (var mapping in simpleMappings)
        {
            if (mapping.targetButton == null) continue;

            if (mapping.pressType == PressType.Normal)
            {
                if (Input.GetKeyDown(mapping.keyCode))
                    SimulateButtonPress(mapping.targetButton, mapping.invokeOnClick, false);
            }
            else // Hold
            {
                if (Input.GetKeyDown(mapping.keyCode))
                {
                    // Start hold: apply visual effect, invoke onClick once
                    ApplyHoldStart(mapping.targetButton, mapping.invokeOnClick);
                }
                else if (Input.GetKeyUp(mapping.keyCode))
                {
                    // Release hold: restore color
                    ApplyHoldEnd(mapping.targetButton);
                }
            }
        }

        // Process complex mappings (Shift+E etc.)
        foreach (var mapping in complexMappings)
        {
            if (mapping.targetButton == null) continue;

            bool modifierPressed = Input.GetKey(mapping.modifierKey);
            bool mainKeyDown = Input.GetKeyDown(mapping.mainKey);
            bool mainKeyUp = Input.GetKeyUp(mapping.mainKey);

            if (mapping.pressType == PressType.Normal)
            {
                if (modifierPressed && mainKeyDown)
                    SimulateButtonPress(mapping.targetButton, mapping.invokeOnClick, false);
            }
            else // Hold
            {
                if (modifierPressed && mainKeyDown)
                {
                    ApplyHoldStart(mapping.targetButton, mapping.invokeOnClick);
                }
                else if (!modifierPressed && mainKeyUp)
                {
                    ApplyHoldEnd(mapping.targetButton);
                }

                if (heldButtons.Contains(mapping.targetButton))
                {
                    if (!(Input.GetKey(mapping.modifierKey) && Input.GetKey(mapping.mainKey)))
                    {
                        ApplyHoldEnd(mapping.targetButton);
                    }
                }
            }
        }
    }

    private void SimulateButtonPress(Button button, bool invokeOnClick, bool isHold = false)
    {
        // Visual effect
        if (isHold)
            StartCoroutine(ApplyHoldEffect(button));
        else
            StartCoroutine(ApplyPressEffect(button));

        if (invokeOnClick && button.interactable && button.gameObject.activeInHierarchy)
            button.onClick.Invoke();
    }

    private void ApplyHoldStart(Button button, bool invokeOnClick)
    {
        if (heldButtons.Contains(button)) return;

        var graphic = button.targetGraphic;
        if (graphic != null)
            graphic.color = pressColor;

        heldButtons.Add(button);

        if (invokeOnClick && button.interactable && button.gameObject.activeInHierarchy)
            button.onClick.Invoke();
    }

    private void ApplyHoldEnd(Button button)
    {
        if (!heldButtons.Contains(button)) return;

        var graphic = button.targetGraphic;
        if (graphic != null && originalColors.TryGetValue(button, out Color original))
            graphic.color = original;

        heldButtons.Remove(button);
    }

    private IEnumerator ApplyPressEffect(Button button)
    {
        var graphic = button.targetGraphic;
        if (graphic == null) yield break;

        Color original;
        if (!originalColors.TryGetValue(button, out original))
        {
            original = graphic.color;
            originalColors[button] = original;
        }

        graphic.color = pressColor;
        yield return new WaitForSecondsRealtime(pressDuration);

        if (graphic != null)
            graphic.color = original;
    }

    private IEnumerator ApplyHoldEffect(Button button)
    {
        yield break;
    }

    public void AddSimpleMapping(KeyCode key, Button button, PressType type = PressType.Normal, bool invoke = true)
    {
        simpleMappings.Add(new KeyButtonMapping
        {
            keyCode = key,
            targetButton = button,
            pressType = type,
            invokeOnClick = invoke
        });

        if (!originalColors.ContainsKey(button))
            StoreOriginalColor(button);
    }

    public void AddComplexMapping(KeyCode modifier, KeyCode main, Button button, PressType type = PressType.Normal, bool invoke = true)
    {
        complexMappings.Add(new ComplexKeyButtonMapping
        {
            modifierKey = modifier,
            mainKey = main,
            targetButton = button,
            pressType = type,
            invokeOnClick = invoke
        });

        if (!originalColors.ContainsKey(button))
            StoreOriginalColor(button);
    }
}