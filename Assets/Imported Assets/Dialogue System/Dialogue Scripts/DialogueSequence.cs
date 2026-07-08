using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue Sequence", menuName = "Dialogue System/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public List<DialogueStep> steps = new List<DialogueStep>();
}