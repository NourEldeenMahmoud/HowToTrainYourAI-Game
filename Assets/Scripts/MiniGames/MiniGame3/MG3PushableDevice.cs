using UnityEngine;

public class MG3PushableDevice : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string deviceId;
    [SerializeField] private string groupId;
    [SerializeField] private int sizeRank;

    [Header("State")]
    [SerializeField] private bool locked;
    [SerializeField] private Vector2Int currentCoordinate;
    [SerializeField] private Vector2Int startingCoordinate;

    [Header("References")]
    [SerializeField] private MG3GridManager gridManager;
    [SerializeField] private Transform visualReference;

    [Header("Lock Visuals")]
    [SerializeField] private Renderer lockedRenderer;
    [SerializeField] private bool useLockVisual = true;
    [SerializeField] private Color lockedColor = new Color(0.35f, 1f, 0.55f, 1f);
    [SerializeField] private bool useEmission = true;
    [SerializeField] private Color lockedEmissionColor = new Color(0f, 0.6f, 0.25f, 1f);

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;
    [SerializeField] private bool autoResolveOccupiedStartCell = true;
    [SerializeField] private bool snapToGridOnStart = false;
    [SerializeField] private bool resetUsingAuthoredWorldPosition = true;

    private Vector3 startingWorldPosition;

    public string DeviceId => deviceId;
    public string GroupId => groupId;
    public int SizeRank => sizeRank;
    public bool IsLocked => locked;
    public Vector2Int CurrentCoordinate => currentCoordinate;
    public Vector2Int StartingCoordinate => startingCoordinate;
    public Transform VisualReference => visualReference != null ? visualReference : transform;

    private void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<MG3GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogError("[MG3PushableDevice] Missing MG3GridManager reference.", this);
            enabled = false;
            return;
        }

        if (gridManager.TileCount == 0)
        {
            gridManager.BuildRegistry();
        }

        startingWorldPosition = transform.position;

        currentCoordinate = gridManager.WorldToGrid(transform.position);
        if (!gridManager.IsInBounds(currentCoordinate) && gridManager.TryFindNearestTileCoord(transform.position, out Vector2Int nearest))
        {
            currentCoordinate = nearest;
        }

        // Clear any stale occupancy entry that SyncPushableOccupancyFromScene (called from
        // MG3RobotGridMover.Start) may have written for this device before our own Start()
        // ran. Without this, IsCellOccupied below would return true because the cell is
        // occupied by *ourselves*, triggering autoResolveOccupiedStartCell to relocate the
        // device to a wrong tile — leaving the actual visual position walkable and making
        // the push interaction fail.
        gridManager.UnregisterOccupant(this);

        if (autoResolveOccupiedStartCell && gridManager.IsCellOccupied(currentCoordinate))
        {
            if (gridManager.TryFindNearestFreeWalkableCoord(transform.position, out Vector2Int freeCell))
            {
                if (verboseLogs)
                {
                    Debug.Log($"[MG3PushableDevice] Start cell occupied for `{name}`, remapping {currentCoordinate} -> {freeCell}.", this);
                }

                currentCoordinate = freeCell;
            }
        }

        // Only auto-assign startingCoordinate if it wasn't set in the Inspector (still default 0,0)
        if (startingCoordinate == Vector2Int.zero)
        {
            startingCoordinate = currentCoordinate;
        }
        if (snapToGridOnStart)
        {
            transform.position = gridManager.GridToWorld(currentCoordinate);
        }

        MG3GridManager.OccupantKind kind = locked
            ? MG3GridManager.OccupantKind.LockedPushable
            : MG3GridManager.OccupantKind.Pushable;
        if (!gridManager.RegisterOccupant(this, currentCoordinate, kind))
        {
            Debug.LogWarning($"[MG3PushableDevice] Could not register `{name}` at {currentCoordinate}.", this);
        }
        else if (verboseLogs)
        {
            Debug.Log($"[MG3PushableDevice] Registered `{name}` at {currentCoordinate} (locked={locked}).", this);
        }

        // Self-heal immediately
        ValidateOccupancyConsistency();

    }

    private void OnDisable()
    {
        if (gridManager != null)
        {
            gridManager.UnregisterOccupant(this);
        }
    }

    public void SetLocked(bool value)
    {
        if (locked == value) return;

        locked = value;
        if (gridManager == null) return;

        gridManager.UnregisterOccupant(this);
        MG3GridManager.OccupantKind kind = locked
            ? MG3GridManager.OccupantKind.LockedPushable
            : MG3GridManager.OccupantKind.Pushable;
        gridManager.RegisterOccupant(this, currentCoordinate, kind);

        ValidateOccupancyConsistency();
        LogState("After lock");

        // Apply or remove the lock visual whenever the locked state changes.
        ApplyLockVisual(locked);
    }

    public void SetCurrentCoordinate(Vector2Int coord, bool snapTransform = true)
    {
        if (!gridManager.IsInBounds(coord))
        {
            if (gridManager.TryFindNearestTileCoord(transform.position, out Vector2Int nearest))
                coord = nearest;
            else
                return;
        }

        currentCoordinate = coord;
        if (snapTransform && gridManager != null)
        {
            transform.position = gridManager.GridToWorld(coord);
        }

        // Re-register after coordinate change
        gridManager.UnregisterOccupant(this);
        MG3GridManager.OccupantKind kind = locked ? MG3GridManager.OccupantKind.LockedPushable : MG3GridManager.OccupantKind.Pushable;
        gridManager.RegisterOccupant(this, currentCoordinate, kind);
    }

    public void ResetToTaskStart()
    {
        if (gridManager == null) return;

        // Hard guarantee: solved/locked devices must never reset or become pushable again.
        if (locked) return;

        gridManager.UnregisterOccupant(this);
        locked = false;
        if (resetUsingAuthoredWorldPosition)
        {
            transform.position = startingWorldPosition;
            currentCoordinate = gridManager.WorldToGrid(transform.position);
            if (!gridManager.IsInBounds(currentCoordinate) && gridManager.TryFindNearestTileCoord(transform.position, out Vector2Int nearest))
            {
                currentCoordinate = nearest;
            }
        }
        else
        {
            currentCoordinate = startingCoordinate;
            transform.position = startingWorldPosition;
        }

        gridManager.RegisterOccupant(this, currentCoordinate, MG3GridManager.OccupantKind.Pushable);
        ValidateOccupancyConsistency();
        if (verboseLogs) Debug.Log($"[MG3PushableDevice] Reset `{name}` to {currentCoordinate}.", this);
    }

    /// <summary>
    /// Ensures the grid manager occupancy matches the device current locked state and coordinate.
    /// Auto-corrects out-of-bounds coordinates.
    /// </summary>
    public void ValidateOccupancyConsistency()
    {
        if (gridManager == null) return;

        Vector2Int coord = currentCoordinate;

        // Auto-correct out-of-bounds coordinates
        if (!gridManager.IsInBounds(coord))
        {
            if (gridManager.TryFindNearestTileCoord(transform.position, out Vector2Int nearest))
            {
                coord = nearest;
                currentCoordinate = coord; // update device
                if (snapToGridOnStart) transform.position = gridManager.GridToWorld(coord);
                Debug.Log($"[MG3PushableDevice] Auto-corrected out-of-bounds `{name}` from {currentCoordinate} to {coord}");
            }
            else
            {
                Debug.LogWarning($"[MG3PushableDevice] `{name}` at {coord} is out of bounds and no nearest tile found.", this);
                return;
            }
        }

        bool hasCorrectOccupant = gridManager.TryGetOccupantHandle(coord, out Object handle) && handle == this;
        if (!hasCorrectOccupant)
        {
            MG3GridManager.OccupantKind kind = locked ? MG3GridManager.OccupantKind.LockedPushable : MG3GridManager.OccupantKind.Pushable;
            gridManager.UnregisterOccupant(this);
            if (gridManager.RegisterOccupant(this, coord, kind))
            {
                Debug.Log($"[FIX] Repaired occupancy for `{name}` at {coord} as {kind}");
            }
            else
            {
                Debug.LogError($"[FIX] Failed to repair occupancy for `{name}` at {coord}");
            }
        }
        else
        {
            // Verify occupant kind matches lock state
            if (gridManager.TryGetOccupantKind(coord, out MG3GridManager.OccupantKind existingKind))
            {
                MG3GridManager.OccupantKind expectedKind = locked ? MG3GridManager.OccupantKind.LockedPushable : MG3GridManager.OccupantKind.Pushable;
                if (existingKind != expectedKind)
                {
                    gridManager.UnregisterOccupant(this);
                    gridManager.RegisterOccupant(this, coord, expectedKind);
                    Debug.Log($"[FIX] Corrected occupant kind for `{name}` at {coord} from {existingKind} to {expectedKind}");
                }
            }
        }
    }

    private void LogState(string context)
    {
        if (!verboseLogs) return;
        string occupantKind = "none";
        if (gridManager != null && gridManager.TryGetOccupantKind(currentCoordinate, out MG3GridManager.OccupantKind kind))
            occupantKind = kind.ToString();
        Debug.Log($"[Frame {Time.frameCount}] {context}: `{name}` locked={locked}, coord={currentCoordinate}, occupantKind={occupantKind}");
    }

    /// <summary>
    /// Applies or removes the lock visual on the device renderer using a
    /// MaterialPropertyBlock so that the shared material is never modified.
    /// </summary>
    private void ApplyLockVisual(bool isLocked)
    {
        if (!useLockVisual) return;

        // Resolve the renderer: use the explicitly assigned one first, then fall
        // back to any renderer on the visual reference transform or this transform.
        Renderer r = lockedRenderer;
        if (r == null && visualReference != null)
        {
            r = visualReference.GetComponentInChildren<Renderer>(true);
        }
        if (r == null)
        {
            r = GetComponentInChildren<Renderer>(true);
        }

        if (r == null)
        {
            if (verboseLogs)
            {
                Debug.LogWarning($"[MG3PushableDevice] `{name}`: No renderer found for lock visual.", this);
            }
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);

        if (isLocked)
        {
            block.SetColor("_Color", lockedColor);
            if (useEmission)
            {
                block.SetColor("_EmissionColor", lockedEmissionColor);
                // Enable emission keyword so the color is actually visible at runtime.
                r.material.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            // Restore default block state (removes overrides).
            block = new MaterialPropertyBlock();
            if (useEmission)
            {
                r.material.DisableKeyword("_EMISSION");
            }
        }

        r.SetPropertyBlock(block);
    }
}
