using UnityEngine;

public class MG3LabObstacleRegistrar : MonoBehaviour
{
    [SerializeField] private Transform labRoot;
    [SerializeField] private MG3GridManager gridManager;

    private bool registered;

    public void Register()
    {
        if (registered) return;
        if (gridManager == null) gridManager = FindFirstObjectByType<MG3GridManager>();
        if (gridManager == null || labRoot == null) return;

        gridManager.RegisterLabObstacles(labRoot);
        registered = true;
    }

    public void Restore()
    {
        if (gridManager == null) return;
        gridManager.RestoreLabObstacles();
    }
}
