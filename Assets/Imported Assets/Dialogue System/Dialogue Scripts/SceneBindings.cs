using System.Collections.Generic;
using UnityEngine;

public class SceneBindings : MonoBehaviour
{
    [Header("Cameras")]
    public List<Camera> cameras = new List<Camera>();

    [Header("Characters")]
    public List<GameObject> characters = new List<GameObject>();

    public Camera GetCamera(int id)
    {
        if (id >= 0 && id < cameras.Count)
            return cameras[id];
        return null;
    }

    public GameObject GetCharacter(int id)
    {
        if (id >= 0 && id < characters.Count)
            return characters[id];
        return null;
    }
}