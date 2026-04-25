using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MG2MovableTileHighlighter : MonoBehaviour
{
    private sealed class MarkerVisual
    {
        public Transform root;
        public LineRenderer ring;
        public MeshRenderer dotRenderer;
        public float pulsePhase;
    }

    [Header("References")]
    [SerializeField] private MiniGame2Manager miniGame2Manager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TileClickMover tileClickMover;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindReferences = true;

    [Header("Visual")]
    [SerializeField, Range(0.2f, 0.9f)] private float highlightScaleRelativeToTile = 0.55f;
    [SerializeField, Range(0.01f, 0.2f)] private float ringThicknessRelativeToTile = 0.045f;
    [SerializeField, Range(0.05f, 0.4f)] private float centerDotScaleRelative = 0.16f;
    [SerializeField] private Color ringColor = new Color(0.14f, 0.96f, 1f, 1f);
    [SerializeField] private Color dotColor = new Color(0.60f, 1f, 1f, 1f);
    [SerializeField] private bool useTransparentMaterial = true;
    [SerializeField] private int markerLayer = 0;

    [Header("Placement")]
    [SerializeField, Min(0f)] private float yOffset = 0.24f;
    [SerializeField, Min(0f)] private float minHeightAboveGrid = 0.20f;
    [SerializeField, Min(0.5f)] private float surfaceSnapRayHeight = 8f;
    [SerializeField] private LayerMask surfaceSnapMask = ~0;

    [Header("Animation")]
    [SerializeField] private bool animatePulse = true;
    [SerializeField, Min(0f)] private float pulseSpeed = 2.2f;
    [SerializeField, Range(0f, 0.2f)] private float pulseScaleAmplitude = 0.05f;
    [SerializeField, Min(0f)] private float idleSpinDegreesPerSecond = 42f;

    [Header("Behavior")]
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.12f;

    private readonly List<MarkerVisual> markerPool = new List<MarkerVisual>(8);
    private Transform markerRoot;
    private Material ringMaterial;
    private Material dotMaterial;
    private float refreshTimer;
    private int activeMarkerCount;
    private int cachedFloorLayer = -1;

    private void Awake()
    {
        cachedFloorLayer = LayerMask.NameToLayer("Floor");
        ResolveReferences();
        EnsureMarkerRoot();
        EnsureMaterials();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureMarkerRoot();
        EnsureMaterials();
        Subscribe();
        StartCoroutine(RefreshOnNextFrame());
    }

    private void OnDisable()
    {
        Unsubscribe();
        SetActiveMarkerCount(0);
    }

    private void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
        if (dotMaterial != null) Destroy(dotMaterial);
    }

    private void Update()
    {
        if (autoFindReferences && (miniGame2Manager == null || gridManager == null || tileClickMover == null))
            ResolveReferences();

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshHighlights();
        }

        AnimateMarkers();
    }

    private IEnumerator RefreshOnNextFrame()
    {
        yield return null;
        RefreshHighlights();
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences)
            return;

        if (miniGame2Manager == null)
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
        if (tileClickMover == null)
            tileClickMover = FindFirstObjectByType<TileClickMover>();
    }

    private void Subscribe()
    {
        if (tileClickMover != null)
            tileClickMover.StepCompleted += OnStepCompleted;

        if (miniGame2Manager != null)
            miniGame2Manager.PhaseChanged += OnPhaseChanged;
    }

    private void Unsubscribe()
    {
        if (tileClickMover != null)
            tileClickMover.StepCompleted -= OnStepCompleted;

        if (miniGame2Manager != null)
            miniGame2Manager.PhaseChanged -= OnPhaseChanged;
    }

    private void OnStepCompleted(Vector2Int _, float __)
    {
        RefreshHighlights();
    }

    private void OnPhaseChanged(MiniGame2Phase _)
    {
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        if (!CanShowHighlights())
        {
            SetActiveMarkerCount(0);
            return;
        }

        Vector2Int center = tileClickMover.CurrentGridPos;
        bool diagonal = tileClickMover.AllowDiagonalAdjacent;

        if (gridManager.GetNode(center.x, center.y) == null)
        {
            SetActiveMarkerCount(0);
            return;
        }

        int activeIndex = 0;
        activeIndex = TryPlaceIfLegal(center + Vector2Int.right, activeIndex);
        activeIndex = TryPlaceIfLegal(center + Vector2Int.left, activeIndex);
        activeIndex = TryPlaceIfLegal(center + Vector2Int.up, activeIndex);
        activeIndex = TryPlaceIfLegal(center + Vector2Int.down, activeIndex);

        if (diagonal)
        {
            activeIndex = TryPlaceIfLegal(center + new Vector2Int(1, 1), activeIndex);
            activeIndex = TryPlaceIfLegal(center + new Vector2Int(1, -1), activeIndex);
            activeIndex = TryPlaceIfLegal(center + new Vector2Int(-1, 1), activeIndex);
            activeIndex = TryPlaceIfLegal(center + new Vector2Int(-1, -1), activeIndex);
        }

        SetActiveMarkerCount(activeIndex);
    }

    private int TryPlaceIfLegal(Vector2Int coord, int activeIndex)
    {
        GridManager.Node node = gridManager.GetNode(coord.x, coord.y);
        if (node == null || node.isBlocked)
            return activeIndex;

        if (!TryGetFloorSurfaceY(coord, out float y))
            return activeIndex;

        MarkerVisual marker = GetMarker(activeIndex);
        Vector3 center = gridManager.GridToWorld(coord.x, coord.y);
        marker.root.position = new Vector3(center.x, y, center.z);
        marker.root.gameObject.SetActive(true);
        return activeIndex + 1;
    }

    private bool TryGetFloorSurfaceY(Vector2Int coord, out float y)
    {
        Vector3 center = gridManager.GridToWorld(coord.x, coord.y);
        Vector3 rayOrigin = center + Vector3.up * surfaceSnapRayHeight;
        float rayDistance = surfaceSnapRayHeight * 2f + 10f;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, surfaceSnapMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            y = 0f;
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
                continue;

            FloorTile floor = hit.collider.GetComponentInParent<FloorTile>();
            bool onFloorLayer = cachedFloorLayer >= 0 && hit.collider.gameObject.layer == cachedFloorLayer;
            if (floor == null && !onFloorLayer)
                continue;

            y = Mathf.Max(hit.point.y + yOffset, center.y + minHeightAboveGrid);
            return true;
        }

        y = 0f;
        return false;
    }

    private float ResolveMarkerY(Vector3 cellCenter)
    {
        float fallback = cellCenter.y + minHeightAboveGrid + yOffset;
        Vector3 rayOrigin = cellCenter + Vector3.up * surfaceSnapRayHeight;
        float rayDistance = surfaceSnapRayHeight * 2f + 10f;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, surfaceSnapMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return fallback;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
                continue;

            FloorTile floor = hit.collider.GetComponentInParent<FloorTile>();
            if (floor == null)
                continue;

            float y = hit.point.y + yOffset;
            return Mathf.Max(y, cellCenter.y + minHeightAboveGrid);
        }

        return fallback;
    }

    private bool CanShowHighlights()
    {
        if (gridManager == null || tileClickMover == null)
            return false;

        if (miniGame2Manager == null)
            return true;

        return miniGame2Manager.CurrentPhase != MiniGame2Phase.Completed;
    }

    private void EnsureMarkerRoot()
    {
        if (markerRoot != null)
            return;

        GameObject root = new GameObject("MG2_MovableTileHighlights");
        root.transform.SetParent(transform, false);
        root.layer = markerLayer;
        markerRoot = root.transform;
    }

    private void EnsureMaterials()
    {
        if (ringMaterial != null && dotMaterial != null)
            return;

        Shader ringShader = Shader.Find("Sprites/Default");
        if (ringShader == null) ringShader = Shader.Find("Unlit/Color");
        if (ringShader == null) ringShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (ringShader == null) ringShader = Shader.Find("Standard");

        Shader dotShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (dotShader == null) dotShader = Shader.Find("Unlit/Color");
        if (dotShader == null) dotShader = Shader.Find("Sprites/Default");
        if (dotShader == null) dotShader = Shader.Find("Standard");
        if (ringShader == null || dotShader == null) return;

        ringMaterial = new Material(ringShader) { name = "MG2_MoveRing_Mat" };
        dotMaterial = new Material(dotShader) { name = "MG2_MoveDot_Mat" };

        SetupMaterial(ringMaterial, ringColor, 1.2f);
        SetupMaterial(dotMaterial, dotColor, 1.6f);
    }

    private void SetupMaterial(Material mat, Color color, float emissionMul)
    {
        if (mat == null)
            return;

        if (useTransparentMaterial)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionMul);
        }
    }

    private MarkerVisual GetMarker(int index)
    {
        while (markerPool.Count <= index)
            markerPool.Add(CreateMarker(markerPool.Count));

        return markerPool[index];
    }

    private MarkerVisual CreateMarker(int index)
    {
        float cellSize = GetCellSize();
        float side = Mathf.Max(0.1f, cellSize * highlightScaleRelativeToTile);
        float radius = side * 0.46f;

        GameObject rootGo = new GameObject("MoveMarker_" + index);
        rootGo.transform.SetParent(markerRoot, false);
        rootGo.layer = markerLayer;

        GameObject ringGo = new GameObject("Ring");
        ringGo.transform.SetParent(rootGo.transform, false);
        ringGo.layer = markerLayer;
        LineRenderer ring = ringGo.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 42;
        ring.alignment = LineAlignment.TransformZ;
        ring.numCapVertices = 6;
        ring.numCornerVertices = 6;
        ring.textureMode = LineTextureMode.Stretch;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.generateLightingData = false;
        float width = Mathf.Max(0.01f, cellSize * ringThicknessRelativeToTile);
        ring.startWidth = width;
        ring.endWidth = width;
        ring.widthMultiplier = 1f;
        ring.sortingOrder = 100;
        if (ringMaterial != null)
            ring.sharedMaterial = ringMaterial;

        Vector3[] points = new Vector3[ring.positionCount];
        float step = Mathf.PI * 2f / ring.positionCount;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i * step;
            points[i] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
        }
        ring.SetPositions(points);
        ring.startColor = ringColor;
        ring.endColor = ringColor;

        GameObject dotGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dotGo.name = "CenterDot";
        dotGo.transform.SetParent(rootGo.transform, false);
        dotGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        dotGo.transform.localScale = Vector3.one * (side * centerDotScaleRelative);
        dotGo.layer = markerLayer;

        Collider col = dotGo.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Destroy(col);
        }

        MeshRenderer dotRenderer = dotGo.GetComponent<MeshRenderer>();
        if (dotRenderer != null && dotMaterial != null)
        {
            dotRenderer.sharedMaterial = dotMaterial;
            dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dotRenderer.receiveShadows = false;
        }

        rootGo.transform.localScale = Vector3.one;
        rootGo.SetActive(false);

        return new MarkerVisual
        {
            root = rootGo.transform,
            ring = ring,
            dotRenderer = dotRenderer,
            pulsePhase = Random.value * Mathf.PI * 2f
        };
    }

    private float GetCellSize()
    {
        if (gridManager == null)
            return 1f;

        Vector3 a = gridManager.GridToWorld(0, 0);
        Vector3 b = gridManager.GridToWorld(1, 0);
        float size = Vector3.Distance(a, b);
        return size > 0.0001f ? size : 1f;
    }

    private void SetActiveMarkerCount(int count)
    {
        activeMarkerCount = Mathf.Clamp(count, 0, markerPool.Count);
        for (int i = 0; i < markerPool.Count; i++)
            markerPool[i].root.gameObject.SetActive(i < count);
    }

    private void AnimateMarkers()
    {
        if (activeMarkerCount <= 0)
            return;

        float cellSize = GetCellSize();
        float baseScale = Mathf.Max(0.1f, cellSize * highlightScaleRelativeToTile);
        float spinDelta = idleSpinDegreesPerSecond * Time.deltaTime;

        for (int i = 0; i < activeMarkerCount; i++)
        {
            MarkerVisual marker = markerPool[i];
            if (marker == null || marker.root == null || !marker.root.gameObject.activeSelf)
                continue;

            float pulse = 1f;
            if (animatePulse)
                pulse += Mathf.Sin(Time.time * pulseSpeed + marker.pulsePhase) * pulseScaleAmplitude;

            float scaled = baseScale * pulse;
            marker.root.localScale = new Vector3(scaled, scaled, scaled);
            marker.root.Rotate(Vector3.up, spinDelta, Space.Self);
        }
    }
}

public static class MG2MovableTileHighlighterBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        MiniGame2Manager manager = Object.FindFirstObjectByType<MiniGame2Manager>();
        MG2MovableTileHighlighter existing = Object.FindFirstObjectByType<MG2MovableTileHighlighter>();
        if (existing != null)
            return;

        if (manager != null)
        {
            manager.gameObject.AddComponent<MG2MovableTileHighlighter>();
            return;
        }

        TileClickMover mover = Object.FindFirstObjectByType<TileClickMover>();
        if (mover != null)
            mover.gameObject.AddComponent<MG2MovableTileHighlighter>();
    }
}
