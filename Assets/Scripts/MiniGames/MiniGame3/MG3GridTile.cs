using UnityEngine;

public class MG3GridTile : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2Int coordinate;
    [SerializeField] private bool walkable = true;

    [Header("Debug")]
    [SerializeField] private bool markAsDeadlockRisk;
    [SerializeField] private Color tileColor = new Color(0.2f, 0.7f, 1f, 0.3f);

    public Vector2Int Coordinate
    {
        get => coordinate;
        set => coordinate = value;
    }

    public bool Walkable
    {
        get => walkable;
        set => walkable = value;
    }

    public bool MarkAsDeadlockRisk
    {
        get => markAsDeadlockRisk;
        set => markAsDeadlockRisk = value;
    }

    public Color TileColor => tileColor;
}
