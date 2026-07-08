using UnityEngine;

[DisallowMultipleComponent]
public class MG3SelectedMoveMarker : MonoBehaviour
{
    private sealed class MarkerVisual
    {
        public Transform Root;
        public LineRenderer Ring;
        public MeshRenderer DotRenderer;
        public float PulsePhase;
    }

    [Header("References")]
    [SerializeField] private MG3GridManager gridManager;
    [SerializeField] private MG3RobotGridMover robotMover;

    [Header("Auto Find")]
    [SerializeField] private bool autoFindReferences = true;

    [Header("Visual")]
    [SerializeField, Range(0.2f, 0.9f)] private float markerScaleRelativeToTile = 0.55f;
    [SerializeField, Range(0.01f, 0.2f)] private float ringThicknessRelativeToTile = 0.045f;
    [SerializeField, Range(0.05f, 0.4f)] private float centerDotScaleRelative = 0.16f;
    [SerializeField] private Color ringColor = new Color(0.14f, 0.96f, 1f, 1f);
    [SerializeField] private Color dotColor = new Color(0.60f, 1f, 1f, 1f);
    [SerializeField] private bool useTransparentMaterial = true;
    [SerializeField] private int markerLayer;

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

    private MarkerVisual marker;
    private Material ringMaterial;
    private Material dotMaterial;
    private int cachedFloorLayer = -1;

    private void Awake()
    {
        cachedFloorLayer = LayerMask.NameToLayer("Floor");
        ResolveReferences();
        EnsureMaterials();
        EnsureMarker();
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureMaterials();
        EnsureMarker();
        Subscribe();
        HideMarker();
    }

    private void OnDisable()
    {
        Unsubscribe();
        HideMarker();
    }

    private void OnDestroy()
    {
        if (ringMaterial != null) Destroy(ringMaterial);
        if (dotMaterial != null) Destroy(dotMaterial);
    }

    private void Update()
    {
        if (autoFindReferences && (gridManager == null || robotMover == null))
        {
            ResolveReferences();
            Subscribe();
        }

        AnimateMarker();
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences)
        {
            return;
        }

        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<MG3GridManager>();
        }

        if (robotMover == null)
        {
            robotMover = FindAnyObjectByType<MG3RobotGridMover>();
        }
    }

    private void Subscribe()
    {
        if (robotMover == null)
        {
            return;
        }

        robotMover.DestinationAccepted -= OnDestinationAccepted;
        robotMover.DestinationRejected -= OnDestinationRejected;
        robotMover.DestinationReached -= OnDestinationReached;

        robotMover.DestinationAccepted += OnDestinationAccepted;
        robotMover.DestinationRejected += OnDestinationRejected;
        robotMover.DestinationReached += OnDestinationReached;
    }

    private void Unsubscribe()
    {
        if (robotMover == null)
        {
            return;
        }

        robotMover.DestinationAccepted -= OnDestinationAccepted;
        robotMover.DestinationRejected -= OnDestinationRejected;
        robotMover.DestinationReached -= OnDestinationReached;
    }

    private void OnDestinationAccepted(Vector2Int destination)
    {
        ShowMarker(destination);
    }

    private void OnDestinationRejected(Vector2Int _, string __)
    {
        HideMarker();
    }

    private void OnDestinationReached(Vector2Int _)
    {
        HideMarker();
    }

    private void ShowMarker(Vector2Int coord)
    {
        if (gridManager == null || marker == null || !gridManager.IsInBounds(coord))
        {
            HideMarker();
            return;
        }

        Vector3 center = gridManager.GridToWorld(coord);
        float y = ResolveMarkerY(center);
        marker.Root.position = new Vector3(center.x, y, center.z);
        marker.Root.gameObject.SetActive(true);
    }

    private void HideMarker()
    {
        if (marker != null && marker.Root != null)
        {
            marker.Root.gameObject.SetActive(false);
        }
    }

    private float ResolveMarkerY(Vector3 cellCenter)
    {
        float fallback = cellCenter.y + minHeightAboveGrid + yOffset;
        Vector3 rayOrigin = cellCenter + Vector3.up * surfaceSnapRayHeight;
        float rayDistance = surfaceSnapRayHeight * 2f + 10f;

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, surfaceSnapMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return fallback;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null)
            {
                continue;
            }

            bool isMg3Tile = hit.collider.GetComponentInParent<MG3GridTile>() != null;
            bool isFloorLayer = cachedFloorLayer >= 0 && hit.collider.gameObject.layer == cachedFloorLayer;
            if (!isMg3Tile && !isFloorLayer)
            {
                continue;
            }

            return Mathf.Max(hit.point.y + yOffset, cellCenter.y + minHeightAboveGrid);
        }

        return fallback;
    }

    private void EnsureMaterials()
    {
        if (ringMaterial != null && dotMaterial != null)
        {
            return;
        }

        Shader ringShader = Shader.Find("Sprites/Default");
        if (ringShader == null) ringShader = Shader.Find("Unlit/Color");
        if (ringShader == null) ringShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (ringShader == null) ringShader = Shader.Find("Standard");

        Shader dotShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (dotShader == null) dotShader = Shader.Find("Unlit/Color");
        if (dotShader == null) dotShader = Shader.Find("Sprites/Default");
        if (dotShader == null) dotShader = Shader.Find("Standard");
        if (ringShader == null || dotShader == null) return;

        ringMaterial = new Material(ringShader) { name = "MG3_MoveRing_Mat" };
        dotMaterial = new Material(dotShader) { name = "MG3_MoveDot_Mat" };

        SetupMaterial(ringMaterial, ringColor, 1.2f);
        SetupMaterial(dotMaterial, dotColor, 1.6f);
    }

    private void SetupMaterial(Material mat, Color color, float emissionMul)
    {
        if (mat == null)
        {
            return;
        }

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

    private void EnsureMarker()
    {
        if (marker != null)
        {
            return;
        }

        float cellSize = GetCellSize();
        float side = Mathf.Max(0.1f, cellSize * markerScaleRelativeToTile);
        float radius = side * 0.46f;

        GameObject rootGo = new GameObject("MG3_SelectedMoveMarker");
        rootGo.transform.SetParent(transform, false);
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
        {
            ring.sharedMaterial = ringMaterial;
        }

        Vector3[] points = new Vector3[ring.positionCount];
        float step = Mathf.PI * 2f / ring.positionCount;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * step;
            points[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
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

        rootGo.SetActive(false);
        marker = new MarkerVisual
        {
            Root = rootGo.transform,
            Ring = ring,
            DotRenderer = dotRenderer,
            PulsePhase = Random.value * Mathf.PI * 2f
        };
    }

    private float GetCellSize()
    {
        if (gridManager == null)
        {
            return 1f;
        }

        Vector3 a = gridManager.GridToWorld(Vector2Int.zero);
        Vector3 b = gridManager.GridToWorld(Vector2Int.right);
        float size = Vector3.Distance(a, b);
        return size > 0.0001f ? size : 1f;
    }

    private void AnimateMarker()
    {
        if (marker == null || marker.Root == null || !marker.Root.gameObject.activeSelf)
        {
            return;
        }

        float cellSize = GetCellSize();
        float baseScale = Mathf.Max(0.1f, cellSize * markerScaleRelativeToTile);
        float pulse = 1f;
        if (animatePulse)
        {
            pulse += Mathf.Sin(Time.time * pulseSpeed + marker.PulsePhase) * pulseScaleAmplitude;
        }

        float scaled = baseScale * pulse;
        marker.Root.localScale = new Vector3(scaled, scaled, scaled);
        marker.Root.Rotate(Vector3.up, idleSpinDegreesPerSecond * Time.deltaTime, Space.Self);
    }
}

public static class MG3SelectedMoveMarkerBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        MG3SelectedMoveMarker existing = Object.FindAnyObjectByType<MG3SelectedMoveMarker>();
        if (existing != null)
        {
            return;
        }

        MiniGame3Manager manager = Object.FindAnyObjectByType<MiniGame3Manager>();
        if (manager != null)
        {
            manager.gameObject.AddComponent<MG3SelectedMoveMarker>();
            return;
        }

        MG3RobotGridMover mover = Object.FindAnyObjectByType<MG3RobotGridMover>();
        if (mover != null)
        {
            mover.gameObject.AddComponent<MG3SelectedMoveMarker>();
        }
    }
}
