using System;
using UnityEngine;

[Serializable]
public class NpcCreationData
{
    public Vector3 npcPosition;
    public NpcAnimationData npcAnimationData;
    public Interaction npcInteraction; 
    [Tooltip("Only use for base dialogue interaction. if so, leave interaction field null")]
    public string npcInteractionMessage = "";
}