using System;
using UnityEngine;

[Serializable]
public class DialogueStep
{
    [Header("Dialogue")]
    public string characterName;

    [TextArea(3, 10)]
    public string dialogueText;

    [Header("Scene Binding")]
    public int cameraID;
    public int characterID;

    [Header("Scene Transition")]
    public bool loadNewScene;
    public int targetSceneIndex;
    public string targetSceneName;
    public bool skipMainMenuOnLoad;
}
