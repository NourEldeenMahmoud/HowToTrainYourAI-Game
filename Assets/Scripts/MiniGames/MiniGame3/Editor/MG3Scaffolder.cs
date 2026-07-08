#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Editor utility to create MG3 scene root objects and assist Stage 1 wiring.
public static class MG3Scaffolder
{
    [MenuItem("Tools/MG3/Setup Scene Roots")]
    public static void SetupSceneRoots()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("MG3 Scaffolder", "No active scene loaded.", "OK");
            return;
        }

        // Safety: only operate when the active scene looks like the MG3 scene or user confirms
        bool looksLikeMG3 = scene.name.Contains("Mini Game 3") || scene.path.Contains("MiniGame 3");
        if (!looksLikeMG3)
        {
            if (!EditorUtility.DisplayDialog("MG3 Scaffolder",
                $"Active scene is '{scene.name}'. This tool is intended for the MG3 scene. Continue?",
                "Proceed", "Cancel"))
            {
                return;
            }
        }

        // Create or find root
        GameObject root = GameObject.Find("MG3_Root");
        if (root == null)
        {
            root = new GameObject("MG3_Root");
            Undo.RegisterCreatedObjectUndo(root, "Create MG3_Root");
            Debug.Log("[MG3Scaffolder] Created MG3_Root");
        }

        // Helper to ensure child root exists
        GameObject EnsureChild(string name)
        {
            Transform t = root.transform.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(root.transform);
            go.transform.localPosition = Vector3.zero;
            Debug.Log($"[MG3Scaffolder] Created {name}");
            return go;
        }

        GameObject gridRoot = EnsureChild("MG3_Grid");
        GameObject tasksRoot = EnsureChild("MG3_Tasks");
        GameObject uiRoot = EnsureChild("MG3_UI");
        GameObject camsRoot = EnsureChild("MG3_Cameras");

        // Reparent common named objects into the MG3 roots if found in scene
        void TryReparent(string objectName, GameObject parent)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null && found.transform.parent != parent.transform)
            {
                Undo.SetTransformParent(found.transform, parent.transform, "Reparent " + objectName);
                Debug.Log($"[MG3Scaffolder] Reparented {objectName} under {parent.name}");
            }
        }

        void TryReparentByPrefabSourceName(string prefabName, GameObject parent)
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject go in allObjects)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (source == null || source.name != prefabName)
                {
                    continue;
                }

                if (go.transform.parent != parent.transform)
                {
                    Undo.SetTransformParent(go.transform, parent.transform, "Reparent " + go.name);
                    Debug.Log($"[MG3Scaffolder] Reparented '{go.name}' (source: {prefabName}) under {parent.name}");
                }
            }
        }

        TryReparent("Grid Floor", gridRoot);
        TryReparent("Task 1 Devices", tasksRoot);
        TryReparent("Task 2 Devices", tasksRoot);
        TryReparent("Task 3 Devices", tasksRoot);
        TryReparent("CameraPivot", camsRoot);
        TryReparent("CM vcam1", camsRoot);
        TryReparent("Main Camera", camsRoot);
        TryReparent("Robot", root);
        TryReparent("Robot Variant Variant", root);
        TryReparentByPrefabSourceName("Robot Variant Variant", root);

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        if (EditorUtility.DisplayDialog("MG3 Scaffolder", "Scene roots created/reparented. Save scene now?", "Save", "No"))
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MG3Scaffolder] Scene saved.");
        }

        EditorUtility.DisplayDialog("MG3 Scaffolder", "MG3 Stage 1 scaffolding complete. See Console for details.", "OK");
    }
}
#endif
