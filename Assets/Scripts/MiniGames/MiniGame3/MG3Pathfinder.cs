using System.Collections.Generic;
using UnityEngine;

public class MG3Pathfinder : MonoBehaviour
{
    [SerializeField] private MG3GridManager gridManager;

    private static readonly Vector2Int[] Neighbors4 =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<MG3GridManager>();
        }
    }

    public bool TryFindPath(Vector2Int start, Vector2Int goal, out List<Vector2Int> path, out string failureReason)
    {
        path = null;
        failureReason = string.Empty;

        if (gridManager == null)
        {
            failureReason = "Grid manager missing";
            return false;
        }

        if (!gridManager.IsInBounds(start))
        {
            failureReason = "Start out of bounds";
            return false;
        }

        if (!gridManager.IsInBounds(goal))
        {
            failureReason = "Destination out of bounds";
            return false;
        }

        if (!gridManager.IsWalkable(goal))
        {
            failureReason = "Destination blocked or occupied";
            return false;
        }

        if (start == goal)
        {
            path = new List<Vector2Int> { start };
            return true;
        }

        var frontier = new Queue<Vector2Int>(128);
        var visited = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>(128);

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            for (int i = 0; i < Neighbors4.Length; i++)
            {
                Vector2Int next = current + Neighbors4[i];
                if (visited.Contains(next))
                {
                    continue;
                }

                if (!gridManager.IsInBounds(next))
                {
                    continue;
                }

                if (next != goal && !gridManager.IsWalkable(next))
                {
                    continue;
                }

                if (next == goal && !gridManager.IsWalkable(goal))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;

                if (next == goal)
                {
                    path = ReconstructPath(start, goal, cameFrom);
                    return true;
                }

                frontier.Enqueue(next);
            }
        }

        failureReason = "No path to destination";
        return false;
    }

    private static List<Vector2Int> ReconstructPath(Vector2Int start, Vector2Int goal, Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        var path = new List<Vector2Int>(64);
        Vector2Int current = goal;
        path.Add(current);

        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
