using System.Collections;
using UnityEngine;

public class MG3TargetSlot : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int coordinate;

    [Header("Requirements")]
    [SerializeField] private string requiredDeviceId;
    [SerializeField] private string requiredGroupId;
    [SerializeField] private int requiredSizeRank;
    [SerializeField] private int sizeOrderIndex;

    [Header("State")]
    [SerializeField] private bool isSolved;

    [Header("Visuals")]
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private Color unsolvedColor = new Color(0.9f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color solvedColor = new Color(0.2f, 0.9f, 0.35f, 1f);
    [SerializeField, Range(1f, 5f)] private float solvedIntensity = 1.5f;
    [SerializeField, Range(0f, 1f)] private float unsolvedAlpha = 0.3f;
    [SerializeField, Min(0.01f)] private float transitionSpeed = 3f;

    private static readonly int BaseColorShaderGraphID = Shader.PropertyToID("baseColorFactor");
    private static readonly int BaseColorURPID = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseColorLegacyID = Shader.PropertyToID("_Color");
    private static readonly int EmissionShaderGraphID = Shader.PropertyToID("emissiveFactor");
    private static readonly int EmissionLegacyID = Shader.PropertyToID("_EmissionColor");

    private Coroutine transitionRoutine;
    private MaterialPropertyBlock propertyBlock;

    public Vector2Int Coordinate => coordinate;
    public string RequiredDeviceId => requiredDeviceId;
    public string RequiredGroupId => requiredGroupId;
    public int RequiredSizeRank => requiredSizeRank;
    public int SizeOrderIndex => sizeOrderIndex;
    public bool IsSolved => isSolved;

    public void SetCoordinate(Vector2Int value)
    {
        coordinate = value;
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        ResolveIndicatorRenderer();
    }

    private void ResolveIndicatorRenderer()
    {
        if (indicatorRenderer != null) return;

        indicatorRenderer = GetComponent<Renderer>();
        if (indicatorRenderer == null)
        {
            indicatorRenderer = GetComponentInChildren<Renderer>();
        }

        if (indicatorRenderer == null)
        {
            Debug.LogWarning($"[MG3TargetSlot] No Renderer found on '{name}' for indicator visual.", this);
        }
    }

    private void Start()
    {
        isSolved = false;
        ApplyVisualState();
    }

    private void OnDestroy()
    {
        StopTransition();
    }

    public void SetSolved(bool solved)
    {
        isSolved = solved;
        ApplyVisualState();
    }

    public void ResetSolvedVisual()
    {
        isSolved = false;
        StopTransition();
        SetColor(unsolvedColor, Color.black, 1f);
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (indicatorRenderer == null) return;

        StopTransition();
        transitionRoutine = StartCoroutine(TransitionToState());
    }

    private IEnumerator TransitionToState()
    {
        Color startColor = GetCurrentColor();
        Color targetColor = isSolved ? solvedColor * solvedIntensity : WithAlpha(unsolvedColor, unsolvedAlpha);
        Color startEmission = GetCurrentEmission();
        Color targetEmission = isSolved ? solvedColor * solvedIntensity * 0.4f : Color.black;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            float t = Mathf.Clamp01(elapsed);
            Color currentColor = Color.Lerp(startColor, targetColor, t);
            Color currentEmission = Color.Lerp(startEmission, targetEmission, t);
            SetColor(currentColor, currentEmission, 1f);
            yield return null;
        }

        SetColor(targetColor, targetEmission, 1f);
        transitionRoutine = null;
    }

    private void SetColor(Color color, Color emission, float emissionIntensity)
    {
        if (indicatorRenderer == null) return;

        indicatorRenderer.GetPropertyBlock(propertyBlock);

        int colorProp = ResolveProperty(indicatorRenderer.material, BaseColorShaderGraphID, BaseColorURPID, BaseColorLegacyID);
        propertyBlock.SetColor(colorProp, color);

        int emissionProp = ResolveProperty(indicatorRenderer.material, EmissionShaderGraphID, EmissionLegacyID);
        propertyBlock.SetColor(emissionProp, emission);

        indicatorRenderer.SetPropertyBlock(propertyBlock);
    }

    private Color GetCurrentColor()
    {
        if (indicatorRenderer == null) return unsolvedColor;

        int colorProp = ResolveProperty(indicatorRenderer.material, BaseColorShaderGraphID, BaseColorURPID, BaseColorLegacyID);
        return indicatorRenderer.material.GetColor(colorProp);
    }

    private Color GetCurrentEmission()
    {
        if (indicatorRenderer == null) return Color.black;

        int emissionProp = ResolveProperty(indicatorRenderer.material, EmissionShaderGraphID, EmissionLegacyID);
        return indicatorRenderer.material.GetColor(emissionProp);
    }

    private static int ResolveProperty(Material mat, int primary, int fallback)
    {
        return mat.HasProperty(primary) ? primary : fallback;
    }

    private static int ResolveProperty(Material mat, int primary, int secondary, int fallback)
    {
        if (mat.HasProperty(primary)) return primary;
        if (mat.HasProperty(secondary)) return secondary;
        return fallback;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void StopTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }
}
