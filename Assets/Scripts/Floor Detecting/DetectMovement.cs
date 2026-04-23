using UnityEngine;

public class DetectMovement : MonoBehaviour
{
    
    [SerializeField] Material colorMaterial;

    [SerializeField] GameObject targetedObject;


    void OnTriggerEnter(Collider other) {
        
        targetedObject.GetComponent<MeshRenderer>().material = colorMaterial;
    }
}
