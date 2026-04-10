using UnityEngine;

public class SimpleInteractable : MonoBehaviour
{
    [SerializeField] private string interactPrompt = "Press E to interact";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private float promptHeightOffset = 1.5f;

    public string InteractPrompt => interactPrompt;
    public Vector3 PromptWorldPosition =>
        promptAnchor != null ? promptAnchor.position : transform.position + Vector3.up * promptHeightOffset;

    public void Interact()
    {
        Debug.Log($"Interacted with: {gameObject.name}");
    }
}
