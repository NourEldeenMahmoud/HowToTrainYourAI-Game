using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public class Node
    {
        public int gridX;
        public int gridY;
        public float movementCost;
        public bool isBlocked;

        // A* bookkeeping
        public float gCost;
        public float hCost;
        public Node parent;

        public float fCost => gCost + hCost;
    }

    [Header("Grid")]
    [SerializeField, Min(1)] private int gridWidth = 40;
    [SerializeField, Min(1)] private int gridHeight = 40;
    [SerializeField] private Vector3 origin = Vector3.zero;
    [Tooltip("If set, grid cell (0,0) is anchored at this transform's world position (overrides origin XZ from serialized origin).")]
    [SerializeField] private Transform gridOriginTransform;
    [SerializeField, Min(0.01f)] private float cellSize = 1f;

    [Header("Scan - Floor")]
    [SerializeField, Min(0.1f)] private float raycastHeight = 5f;
    [SerializeField, Min(0.01f)] private float raycastDistance = 20f;
    [SerializeField, Min(0f)] private float defaultEnergyCost = 1f;

    [Header("Scan - Obstacles")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Vector3 obstacleCheckHalfExtents = new Vector3(0.45f, 1.0f, 0.45f);
    [SerializeField] private Vector3 obstacleCheckOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Pathfinding")]
    [SerializeField] private bool allowDiagonals = true;
    [Tooltip("If true, diagonal moves are blocked if either adjacent cardinal neighbor is blocked.")]
    [SerializeField] private bool preventCornerCutting = true;
    [SerializeField, Min(0f)] private float heuristicMinCost = 1f;

    private Node[,] grid;

    public int Width => gridWidth;
    public int Height => gridHeight;

    private void Start()
    {
        BuildGrid();
    }

    public void BuildGrid()
    {
        if (gridOriginTransform != null)
        {
            Vector3 p = gridOriginTransform.position;
            origin = new Vector3(p.x, origin.y, p.z);
        }

        grid = new Node[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellWorld = GridToWorld(x, y);

                float cost = defaultEnergyCost;
                Vector3 rayOrigin = cellWorld + Vector3.up * raycastHeight;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance))
                {
                    FloorTile tile = hit.collider.GetComponentInParent<FloorTile>();
                    if (tile != null)
                    {
                        cost = tile.EnergyCost;
                    }
                }

                bool blocked = Physics.CheckBox(
                    cellWorld + obstacleCheckOffset,
                    obstacleCheckHalfExtents,
                    Quaternion.identity,
                    obstacleLayer,
                    QueryTriggerInteraction.Ignore
                );

                grid[x, y] = new Node
                {
                    gridX = x,
                    gridY = y,
                    movementCost = Mathf.Max(0f, cost),
                    isBlocked = blocked
                };
            }
        }
    }

    public Node GetNode(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return grid[x, y];
    }

    public List<Vector2Int> FindIdealPath(Vector2Int start, Vector2Int goal)
    {
        if (grid == null || grid.Length == 0)
        {
            BuildGrid();
        }

        Node startNode = GetNode(start.x, start.y);
        Node goalNode = GetNode(goal.x, goal.y);
        if (startNode == null || goalNode == null) return null;
        if (startNode.isBlocked || goalNode.isBlocked) return null;

        // Reset A* book-keeping for all nodes (cheap at 40x40).
        for (int x = 0; x < gridWidth; x++)
        for (int y = 0; y < gridHeight; y++)
        {
            Node n = grid[x, y];
            n.gCost = float.PositiveInfinity;
            n.hCost = 0f;
            n.parent = null;
        }

        var open = new List<Node>(256);
        var closed = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = Heuristic(startNode, goalNode);
        open.Add(startNode);

        while (open.Count > 0)
        {
            Node current = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                Node cand = open[i];
                if (cand.fCost < current.fCost || (Mathf.Approximately(cand.fCost, current.fCost) && cand.hCost < current.hCost))
                {
                    current = cand;
                }
            }

            open.Remove(current);
            closed.Add(current);

            if (current == goalNode)
            {
                return ReconstructPath(startNode, goalNode);
            }

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (neighbor == null || neighbor.isBlocked || closed.Contains(neighbor))
                    continue;

                float tentativeG = current.gCost + neighbor.movementCost;
                if (tentativeG < neighbor.gCost)
                {
                    neighbor.parent = current;
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = Heuristic(neighbor, goalNode);
                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return null;
    }

    public (List<Vector2Int> path, float totalEnergy) FindIdealFullPath(Vector2Int start, Vector2Int card, Vector2Int charger)
    {
        List<Vector2Int> a = FindIdealPath(start, card);
        if (a == null || a.Count == 0) return (null, 0f);

        List<Vector2Int> b = FindIdealPath(card, charger);
        if (b == null || b.Count == 0) return (null, 0f);

        var combined = new List<Vector2Int>(a.Count + b.Count);
        combined.AddRange(a);

        // Avoid duplicating the card tile: b starts with card.
        for (int i = 1; i < b.Count; i++)
            combined.Add(b[i]);

        float energy = GetPathEnergy(combined);
        return (combined, energy);
    }

    public float GetPathEnergy(List<Vector2Int> path)
    {
        if (path == null || path.Count <= 1) return 0f;

        float sum = 0f;
        // By convention, starting tile doesn't cost energy; each step pays the destination tile's cost.
        for (int i = 1; i < path.Count; i++)
        {
            Node n = GetNode(path[i].x, path[i].y);
            if (n != null) sum += n.movementCost;
        }
        return sum;
    }

    private List<Vector2Int> ReconstructPath(Node startNode, Node endNode)
    {
        var path = new List<Vector2Int>(128);
        Node current = endNode;

        while (current != null)
        {
            path.Add(new Vector2Int(current.gridX, current.gridY));
            if (current == startNode) break;
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private IEnumerable<Node> GetNeighbors(Node node)
    {
        // 4-way
        int x = node.gridX;
        int y = node.gridY;

        yield return GetNode(x + 1, y);
        yield return GetNode(x - 1, y);
        yield return GetNode(x, y + 1);
        yield return GetNode(x, y - 1);

        if (!allowDiagonals) yield break;

        // 8-way diagonals
        TryYieldDiagonal(x, y, x + 1, y + 1, x + 1, y, x, y + 1, out Node d1); if (d1 != null) yield return d1;
        TryYieldDiagonal(x, y, x + 1, y - 1, x + 1, y, x, y - 1, out Node d2); if (d2 != null) yield return d2;
        TryYieldDiagonal(x, y, x - 1, y + 1, x - 1, y, x, y + 1, out Node d3); if (d3 != null) yield return d3;
        TryYieldDiagonal(x, y, x - 1, y - 1, x - 1, y, x, y - 1, out Node d4); if (d4 != null) yield return d4;
    }

    private void TryYieldDiagonal(int fromX, int fromY, int diagX, int diagY, int adj1X, int adj1Y, int adj2X, int adj2Y, out Node diag)
    {
        diag = GetNode(diagX, diagY);
        if (diag == null) return;

        if (!preventCornerCutting) return;

        Node adj1 = GetNode(adj1X, adj1Y);
        Node adj2 = GetNode(adj2X, adj2Y);
        if ((adj1 != null && adj1.isBlocked) || (adj2 != null && adj2.isBlocked))
        {
            diag = null;
        }
    }

    private float Heuristic(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        int chebyshev = Mathf.Max(dx, dy);
        return chebyshev * heuristicMinCost;
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < gridWidth && y < gridHeight;
    }

    public Vector3 GridToWorld(int x, int y)
    {
        return origin + new Vector3(x * cellSize, 0f, y * cellSize);
    }

    public Vector2Int WorldToGrid(Vector3 world)
    {
        Vector3 local = world - origin;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.z / cellSize);
        return new Vector2Int(x, y);
    }
}

