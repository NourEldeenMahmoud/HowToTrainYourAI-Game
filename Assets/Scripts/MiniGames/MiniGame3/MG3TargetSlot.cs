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

    private void Start()
    {
        // Always reset to unsolved visual on play so stale serialized state doesn't show green.
        isSolved = false;
        ApplyVisualState();
    }

    public void SetSolved(bool solved)
    {
        isSolved = solved;
        ApplyVisualState();
    }

    public void ResetSolvedVisual()
    {
        isSolved = false;
        ApplyVisualState();
    }

    private void OnValidate()
    {
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (indicatorRenderer == null)
        {
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        indicatorRenderer.GetPropertyBlock(block);
        block.SetColor("_Color", isSolved ? solvedColor : unsolvedColor);
        indicatorRenderer.SetPropertyBlock(block);
    }
}
