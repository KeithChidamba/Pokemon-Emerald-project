using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "npc obj", menuName = "Objectives/Npc Interaction objective")]
public class NpcStoryObjective : StoryObjective
{
    public string npcName;
    public Vector3 npcPosition;
    public GameObject npcPrefab;
    public SpriteDataForNpc npcSprites;
    public Interaction npcInteraction;
    public bool removeAfterInteraction;

    private GameObject npcInstance;
    protected NpcLogic npcLogic;
    
    protected DialogueHandler dialogueHandler;
    protected AreaManager areaManager;

    protected void SetupNpc()
    {
        npcInstance = Instantiate(npcPrefab, npcPosition,npcPrefab.transform.rotation,areaManager.overworldNpcParent);
        npcLogic = npcInstance.GetComponentInChildren<NpcLogic>();
        npcLogic.movementHandler.isControlled = true;
        npcLogic.movementHandler.animationData.isIdle = true;
        npcLogic.movementHandler.animationData.spriteData = npcSprites;
        npcLogic.npcInteractable.interaction = npcInteraction;
        npcInstance.SetActive(true);
    }
    
    protected override void OnObjectiveLoaded()
    {
        dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        areaManager = serviceContainer.Resolve<AreaManager>(); 
        dialogueHandler.DisplayObjectiveText($"Speak to {npcName}");
        SetupNpc();
        dialogueHandler.OnDialogueEnded += ClearAfterInteractionDialogue;
    }
    private void ClearAfterInteractionDialogue(Interaction interaction)
    {
        if (interaction is null) return;
        if (interaction.GetID != npcInteraction.GetID) return; 
        dialogueHandler.OnDialogueEnded -= ClearAfterInteractionDialogue;
        ClearAndHandleRemoval();
    }
    protected void ClearAndHandleRemoval()
    {
        if (removeAfterInteraction)
        {
            Destroy(npcInstance);
        }
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}