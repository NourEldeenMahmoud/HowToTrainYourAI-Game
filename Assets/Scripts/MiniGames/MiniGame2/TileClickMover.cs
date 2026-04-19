using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TileClickMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MiniGame2Manager miniGame2Manager;
    [SerializeField] private Camera raycastCamera;
    [Tooltip("If set, this transform is moved (e.g. the robot root). If null, this object moves itself.")]
    [SerializeField] private Transform moverTransform;

    [Header("Input")]
    [SerializeField] private LayerMask floorLayer;
    [SerializeField, Min(0f)] private float clickMaxDistance = 200f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 3.5f;
    [SerializeField] private bool snapToTileCenter = true;
    [SerializeField] private float arriveDistance = 0.05f;
    [SerializeField] private int maxQueuedTargets = 10;

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleLayer;

    private readonly Queue<Vector2Int> targetQueue = new Queue<Vector2Int>();
    private readonly Queue<Vector2Int> stepQueue = new Queue<Vector2Int>();
    private Coroutine processRoutine;
    private bool isProcessing;

    private Transform Mover => moverTransform != null ? moverTransform : transform;

    private void OnEnable()
    {
        if (processRoutine == null)
            processRoutine = StartCoroutine(ProcessQueue());
    }

    private void OnDisable()
    {
        if (processRoutine != null)
        {
            StopCoroutine(processRoutine);
            processRoutine = null;
        }

        targetQueue.Clear();
        stepQueue.Clear();
        isProcessing = false;
    }

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (miniGame2Manager == null) miniGame2Manager = FindFirstObjectByType<MiniGame2Manager>();
        if (raycastCamera == null) raycastCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryEnqueueClickTarget(Mouse.current.position.ReadValue());
        }
    }

    private void TryEnqueueClickTarget(Vector2 screenPos)
    {
        if (raycastCamera == null) return;

        Ray ray = raycastCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, clickMaxDistance, floorLayer, QueryTriggerInteraction.Ignore))
            return;

        if (gridManager == null) return;
        Vector2Int dest = gridManager.WorldToGrid(hit.point);
        GridManager.Node node = gridManager.GetNode(dest.x, dest.y);
        if (node == null || node.isBlocked) return;

        if (maxQueuedTargets > 0 && targetQueue.Count >= maxQueuedTargets)
            return;

        targetQueue.Enqueue(dest);
        miniGame2Manager?.LogPathEvent($"[MG2] Queued target {dest.x},{dest.y} ({targetQueue.Count}/{maxQueuedTargets})");
    }

    private IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (!isProcessing && (stepQueue.Count > 0 || targetQueue.Count > 0))
            {
                isProcessing = true;
                yield return ProcessOneTarget();
                isProcessing = false;
            }

            yield return null;
        }
    }

    private IEnumerator ProcessOneTarget()
    {
        // If we have no steps, dequeue next target and build an A* step list.
        if (stepQueue.Count == 0)
        {
            if (targetQueue.Count == 0) yield break;
            Vector2Int target = targetQueue.Dequeue();

            if (gridManager == null) yield break;

            Vector2Int start = gridManager.WorldToGrid(Mover.position);
            List<Vector2Int> path = gridManager.FindIdealPath(start, target);
            if (path == null || path.Count < 2)
            {
                miniGame2Manager?.LogPathEvent($"[MG2] No path to {target.x},{target.y}");
                yield break;
            }

            // Enqueue each step excluding the starting tile.
            for (int i = 1; i < path.Count; i++)
                stepQueue.Enqueue(path[i]);
        }

        while (stepQueue.Count > 0)
        {
            Vector2Int step = stepQueue.Dequeue();
            Vector3 worldTarget = gridManager.GridToWorld(step.x, step.y);
            if (snapToTileCenter)
            {
                // Align to the center of the cell on XZ, keep current Y.
                worldTarget = new Vector3(worldTarget.x, Mover.position.y, worldTarget.z);
            }
            else
            {
                worldTarget.y = Mover.position.y;
            }

            // Move until we reach this tile.
            while (Vector3.Distance(Mover.position, worldTarget) > arriveDistance)
            {
                Mover.position = Vector3.MoveTowards(Mover.position, worldTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // Ensure exact snap at the end of each tile.
            Mover.position = worldTarget;

            // Let manager know we're moving / can update phase if needed.
            miniGame2Manager?.NotifyRobotStepProcessed(step);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            miniGame2Manager?.RecordCollision();
        }
    }
}

