using UnityEngine;

public class FloorTile : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField, Min(0f)] private float energyCost = 1f;
    [SerializeField] private bool isEnergySaving;

    [Header("Grid")]
    [Tooltip("If enabled, the tile will compute its grid coordinate from world position (x,z).")]
    [SerializeField] private bool autoGridFromWorld = true;
    [SerializeField] private Vector2Int gridCoord;

    private MiniGame2Manager miniGame2Manager;

    public float EnergyCost => energyCost;
    public bool IsEnergySaving => isEnergySaving;
    public Vector2Int GridCoord => gridCoord;

    public void SetEnergyCost(float cost)
    {
        energyCost = Mathf.Max(0f, cost);
    }

    public void SetEnergySaving(bool saving)
    {
        isEnergySaving = saving;
    }

    private void Awake()
    {
        if (autoGridFromWorld)
        {
            Vector3 p = transform.position;
            gridCoord = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
        }
    }

    private void Start()
    {
        miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (miniGame2Manager == null)
        {
            miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
            if (miniGame2Manager == null) return;
        }

        // Only count robot movement visits (ignore random triggers).
        bool isRobot =
            other.CompareTag("Robot") ||
            other.GetComponentInParent<TileClickMover>() != null ||
            other.GetComponentInParent<RobotMovement>() != null;

        if (!isRobot) return;

        miniGame2Manager.RecordTileVisit(gridCoord, energyCost, isEnergySaving);
    }
}

