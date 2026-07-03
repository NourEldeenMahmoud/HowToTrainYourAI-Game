using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MG3GridManager : MonoBehaviour
{
    public enum OccupantKind
    {
        Robot,
        Pushable,
        LockedPushable,
        StaticBlocked
    }

    [Header("Scene References")]
    [SerializeField] private Transform gridRoot;

    [Header("Grid")]
    [SerializeField] private Vector3 origin = Vector3.zero;
    [SerializeField] private Transform gridOriginTransform;
    [SerializeField, Min(0.01f)] private float cellSize = 2.9f;
    [SerializeField] private float worldY = 0f;
    [SerializeField] private bool useGridRectMask = true;
    [SerializeField, Min(1)] private int gridWidth = 15;
    [SerializeField, Min(1)] private int gridHeight = 17;


    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool verboseLogs;
    [SerializeField] private float gizmoYOffset = 0.1f;
    [SerializeField] private float centerRadius = 0.09f;
    [SerializeField] private Color walkableColor = new Color(0.25f, 0.85f, 0.35f, 0.9f);
    [SerializeField] private Color blockedColor = new Color(0.85f, 0.35f, 0.35f, 0.9f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0.85f, 0.2f, 0.95f);
    [SerializeField] private Color targetSlotColor = new Color(0.2f, 0.75f, 1f, 0.95f);

    private readonly Dictionary<Vector2Int, MG3GridTile> tiles = new Dictionary<Vector2Int, MG3GridTile>();
    private readonly Dictionary<Vector2Int, OccupantKind> occupantsByCell = new Dictionary<Vector2Int, OccupantKind>();
    private readonly Dictionary<Object, Vector2Int> occupantCellsByHandle = new Dictionary<Object, Vector2Int>();
    private readonly Dictionary<Vector2Int, Object> occupantHandlesByCell = new Dictionary<Vector2Int, Object>();
    private readonly HashSet<Vector2Int> targetSlotCells = new HashSet<Vector2Int>();

    public int TileCount => tiles.Count;
    public float CellSize => cellSize;

    private int MinGridX => -Mathf.FloorToInt(gridWidth * 0.5f);
    private int MinGridY => -Mathf.FloorToInt(gridHeight * 0.5f);

    private void Awake()
    {
        ResolveOrigin();
        if (cellSize <= 0f)
        {
            cellSize = 1f;
        }
    }

    private void Start()
    {
        BuildRegistry();
        Debug.Log($"[MG3GridManager] Registered {tiles.Count} grid tiles.", this);
    }

    private void ResolveOrigin()
    {
        if (gridOriginTransform != null)
        {
            Vector3 p = gridOriginTransform.position;
            origin = new Vector3(p.x, origin.y, p.z);
        }
    }

    [ContextMenu("Rebuild Grid Registry")]
    public void BuildRegistry()
    {
        ResolveOrigin();
        tiles.Clear();

        MG3GridTile[] foundTiles = gridRoot == null
            ? Object.FindObjectsByType<MG3GridTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            : gridRoot.GetComponentsInChildren<MG3GridTile>(true);

        int duplicateCount = 0;
        for (int i = 0; i < foundTiles.Length; i++)
        {
            MG3GridTile tile = foundTiles[i];
            if (tile == null)
            {
                continue;
            }

            Vector2Int coord = tile.Coordinate;
            if (useGridRectMask && !IsInsideGridRect(coord))
            {
                continue;
            }

            if (tiles.ContainsKey(coord))
            {
                duplicateCount++;
                Debug.LogError($"[MG3GridManager] Duplicate tile coordinate {coord} found on '{tile.name}'.", tile);
                continue;
            }

            tiles.Add(coord, tile);
        }

        if (duplicateCount > 0)
        {
            Debug.LogWarning($"[MG3GridManager] Grid registry has {duplicateCount} duplicate coordinate conflicts.", this);
        }

        if (verboseLogs)
        {
            Debug.Log($"[MG3GridManager] BuildRegistry found={foundTiles.Length} kept={tiles.Count}", this);
        }
    }

    [ContextMenu("Assign Tile Coordinates From World")]
    public void AssignTileCoordinatesFromWorld()
    {
        ResolveOrigin();

        MG3GridTile[] foundTiles = gridRoot == null
            ? Object.FindObjectsByType<MG3GridTile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            : gridRoot.GetComponentsInChildren<MG3GridTile>(true);

        int assignedCount = 0;
        for (int i = 0; i < foundTiles.Length; i++)
        {
            MG3GridTile tile = foundTiles[i];
            if (tile == null)
            {
                continue;
            }

            tile.Coordinate = WorldToGrid(tile.transform.position);
            assignedCount++;
#if UNITY_EDITOR
            EditorUtility.SetDirty(tile);
#endif
        }

        Debug.Log($"[MG3GridManager] Assigned coordinates for {assignedCount} tiles from world positions.", this);
    }

    public bool TryGetTile(Vector2Int coord, out MG3GridTile tile)
    {
        return tiles.TryGetValue(coord, out tile);
    }

    public bool TryFindNearestTileCoord(Vector3 worldPosition, out Vector2Int nearestCoord)
    {
        nearestCoord = WorldToGrid(worldPosition);
        if (tiles.ContainsKey(nearestCoord))
        {
            return true;
        }

        if (tiles.Count == 0)
        {
            return false;
        }

        float best = float.PositiveInfinity;
        bool found = false;
        foreach (Vector2Int c in tiles.Keys)
        {
            Vector3 p = GridToWorld(c);
            float d = (p - worldPosition).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearestCoord = c;
                found = true;
            }
        }

        return found;
    }

    public bool TryGetTileCoordFromWorld(Vector3 worldPosition, float maxDistance, out Vector2Int coord)
    {
        coord = WorldToGrid(worldPosition);
        if (IsInBounds(coord))
        {
            return true;
        }

        if (!TryFindNearestTileCoord(worldPosition, out Vector2Int nearest))
        {
            return false;
        }

        float allowed = Mathf.Max(0.01f, maxDistance);
        if ((GridToWorld(nearest) - worldPosition).sqrMagnitude > allowed * allowed)
        {
            return false;
        }

        coord = nearest;
        return true;
    }

    public bool TryFindNearestFreeWalkableCoord(Vector3 worldPosition, out Vector2Int coord)
    {
        coord = default;
        if (tiles.Count == 0)
        {
            return false;
        }

        float best = float.PositiveInfinity;
        bool found = false;
        foreach (Vector2Int c in tiles.Keys)
        {
            if (!IsWalkable(c) || IsCellOccupied(c))
            {
                continue;
            }

            float d = (GridToWorld(c) - worldPosition).sqrMagnitude;
            if (d < best)
            {
                best = d;
                coord = c;
                found = true;
            }
        }

        return found;
    }

    public bool IsInBounds(Vector2Int coord)
    {
        if (useGridRectMask && !IsInsideGridRect(coord))
        {
            return false;
        }

        return tiles.ContainsKey(coord);
    }

    public bool IsWalkable(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out MG3GridTile tile) || !tile.Walkable)
        {
            return false;
        }

        if (!occupantsByCell.TryGetValue(coord, out OccupantKind kind))
        {
            return true;
        }

        return kind != OccupantKind.Pushable && kind != OccupantKind.LockedPushable && kind != OccupantKind.StaticBlocked;
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        ResolveOrigin();
        Vector3 local = worldPosition - origin;
        int gx = Mathf.RoundToInt(local.x / cellSize);
        int gy = Mathf.RoundToInt(local.z / cellSize);
        return new Vector2Int(gx, gy);
    }

    public Vector3 GridToWorld(Vector2Int coord)
    {
        ResolveOrigin();
        return origin + new Vector3(coord.x * cellSize, worldY, coord.y * cellSize);
    }

    public Vector3 GetTileWorldPosition(Vector2Int coord)
    {
        if (tiles.TryGetValue(coord, out MG3GridTile tile) && tile != null)
        {
            return tile.transform.position;
        }
        return GridToWorld(coord);
    }

    public bool IsCellOccupied(Vector2Int coord)
    {
        return occupantsByCell.ContainsKey(coord);
    }

    public bool TryGetOccupantKind(Vector2Int coord, out OccupantKind kind)
    {
        return occupantsByCell.TryGetValue(coord, out kind);
    }

    public bool RegisterOccupant(Object handle, Vector2Int coord, OccupantKind kind)
    {
        if (handle == null || !IsInBounds(coord))
        {
            return false;
        }

        // If the cell is already occupied, decide how to proceed.
        if (occupantsByCell.ContainsKey(coord))
        {
            // Idempotent case: this handle is already the occupant at this cell.
            // Just update the kind (e.g., Pushable -> LockedPushable) and return success.
            if (occupantHandlesByCell.TryGetValue(coord, out Object existingHandle) && existingHandle == handle)
            {
                occupantsByCell[coord] = kind;
                return true;
            }

            // Genuine conflict: a different occupant owns this cell. Fail and log so
            // authoring mistakes (two devices at the same tile) are visible in the Console.
            if (verboseLogs && occupantHandlesByCell.TryGetValue(coord, out Object blocker) && blocker != null)
            {
                Debug.LogWarning($"[MG3GridManager] RegisterOccupant: cell {coord} already owned by '{blocker.name}'; cannot register '{handle.name}'.", this);
            }

            return false;
        }

        // Move old cell entry if this handle was previously registered elsewhere.
        if (occupantCellsByHandle.TryGetValue(handle, out Vector2Int previousCell))
        {
            occupantsByCell.Remove(previousCell);
            occupantHandlesByCell.Remove(previousCell);
        }

        occupantCellsByHandle[handle] = coord;
        occupantsByCell[coord] = kind;
        occupantHandlesByCell[coord] = handle;
        return true;
    }

    public bool UnregisterOccupant(Object handle)
    {
        if (handle == null || !occupantCellsByHandle.TryGetValue(handle, out Vector2Int cell))
        {
            return false;
        }

        occupantCellsByHandle.Remove(handle);
        occupantsByCell.Remove(cell);
        occupantHandlesByCell.Remove(cell);
        return true;
    }

    public bool MoveOccupant(Object handle, Vector2Int toCell)
    {
        if (handle == null || !IsInBounds(toCell) || occupantsByCell.ContainsKey(toCell))
        {
            return false;
        }

        if (!occupantCellsByHandle.TryGetValue(handle, out Vector2Int fromCell))
        {
            return false;
        }

        // Do not move locked occupants
        if (occupantsByCell.TryGetValue(fromCell, out OccupantKind kind) && kind == OccupantKind.LockedPushable)
        {
            Debug.LogWarning($"[MG3GridManager] Attempted to move locked occupant {handle.name} from {fromCell} to {toCell}");
            return false;
        }

        OccupantKind kindToMove = occupantsByCell[fromCell];
        occupantsByCell.Remove(fromCell);
        Object handleAtFrom = occupantHandlesByCell[fromCell];
        occupantHandlesByCell.Remove(fromCell);
        occupantsByCell[toCell] = kindToMove;
        occupantHandlesByCell[toCell] = handleAtFrom;
        occupantCellsByHandle[handle] = toCell;
        return true;
    }

    public bool TryGetOccupantDebug(Vector2Int coord, out OccupantKind kind, out string handleName)
    {
        handleName = string.Empty;
        if (!occupantsByCell.TryGetValue(coord, out kind))
        {
            return false;
        }

        if (occupantHandlesByCell.TryGetValue(coord, out Object handle) && handle != null)
        {
            handleName = handle.name;
        }

        return true;
    }

    public bool TryGetOccupantHandle(Vector2Int coord, out Object handle)
    {
        return occupantHandlesByCell.TryGetValue(coord, out handle);
    }

    public void MarkTargetSlot(Vector2Int coord, bool marked)
    {
        if (marked)
        {
            targetSlotCells.Add(coord);
        }
        else
        {
            targetSlotCells.Remove(coord);
        }
    }

    public void ClearAllRuntimeOccupancy()
    {
        occupantsByCell.Clear();
        occupantCellsByHandle.Clear();
        occupantHandlesByCell.Clear();
        targetSlotCells.Clear();
    }

    [ContextMenu("Sync Pushable Occupancy From Scene")]
    public void SyncPushableOccupancyFromScene()
    {
        SyncPushableOccupancyFromScene(true);
    }

    public void SyncPushableOccupancyFromScene(bool logSummary)
    {
        MG3PushableDevice[] devices = Object.FindObjectsByType<MG3PushableDevice>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int synced = 0;

        for (int i = 0; i < devices.Length; i++)
        {
            MG3PushableDevice device = devices[i];
            if (device == null) continue;

            // Always re-derive the correct grid coordinate from the device's current world
            // position. Never trust the serialized CurrentCoordinate field: it may hold a
            // stale value from a previous editor session, or the device may have been moved
            // in the Scene editor without the coordinate being re-baked. Relying on a stale
            // coord registers the device at the wrong cell, leaving its visual position
            // walkable and making it invisible to the push controller.
            Vector2Int coord = WorldToGrid(device.transform.position);
            if (!IsInBounds(coord))
            {
                if (TryFindNearestTileCoord(device.transform.position, out Vector2Int nearest))
                {
                    coord = nearest;
                }
                else
                {
                    Debug.LogWarning($"[MG3GridManager] Device '{device.name}' world position has no matching tile; skipping sync.", this);
                    continue;
                }
            }

            // If the serialized coordinate differs from the world-position-derived one,
            // update the device so Start() and ResetToTaskStart() use the correct value.
            if (device.CurrentCoordinate != coord)
            {
                device.SetCurrentCoordinate(coord, false);
            }

            // Short-circuit if occupancy is already correct.
            bool alreadyCorrect = occupantsByCell.TryGetValue(coord, out OccupantKind existingKind) &&
                                  occupantHandlesByCell.TryGetValue(coord, out Object existingHandle) &&
                                  existingHandle == device &&
                                  ((device.IsLocked && existingKind == OccupantKind.LockedPushable) ||
                                   (!device.IsLocked && existingKind == OccupantKind.Pushable));

            if (!alreadyCorrect)
            {
                UnregisterOccupant(device);
                OccupantKind kind = device.IsLocked ? OccupantKind.LockedPushable : OccupantKind.Pushable;
                if (RegisterOccupant(device, coord, kind))
                {
                    synced++;
                }
            }

            // Self-healing call — verifies the maps are consistent after any changes above.
            device.ValidateOccupancyConsistency();
        }

        if (logSummary)
        {
            Debug.Log($"[MG3GridManager] Synced {synced}/{devices.Length} pushable occupancies from scene.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, MG3GridTile> kvp in tiles)
        {
            Vector2Int coord = kvp.Key;
            MG3GridTile tile = kvp.Value;
            Vector3 world = GridToWorld(coord) + Vector3.up * gizmoYOffset;

            Color baseColor = tile != null && tile.Walkable ? walkableColor : blockedColor;
            if (tile != null && tile.MarkAsDeadlockRisk)
            {
                baseColor = tile.TileColor;
            }

            if (targetSlotCells.Contains(coord))
            {
                baseColor = targetSlotColor;
            }

            Gizmos.color = baseColor;
            Gizmos.DrawWireCube(world, new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));

            if (occupantsByCell.ContainsKey(coord))
            {
                Gizmos.color = occupiedColor;
                Gizmos.DrawSphere(world + Vector3.up * 0.05f, centerRadius);
            }
            else
            {
                Gizmos.DrawSphere(world + Vector3.up * 0.03f, centerRadius * 0.45f);
            }
        }
    }

    private bool IsInsideGridRect(Vector2Int coord)
    {
        int maxX = MinGridX + gridWidth;
        int maxY = MinGridY + gridHeight;
        return coord.x >= MinGridX && coord.y >= MinGridY && coord.x < maxX && coord.y < maxY;
    }
}
