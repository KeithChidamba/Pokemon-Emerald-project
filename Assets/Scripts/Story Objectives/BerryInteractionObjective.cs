using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "berry obj", menuName = "Objectives/berry interaction objective")]
public class BerryInteractionObjective : InteractionObjective
{
    private DialogueHandler _dialogueHandler;
    private OverworldState _overworldState;
    
    public Item berryForObjective;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _overworldState = serviceContainer.Resolve<OverworldState>();
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _dialogueHandler.OnOptionsDisplayed += CheckInteractionTriggered;
    }
    
    private void CheckInteractionTriggered(OverworldInteractable interactable)
    {
        if (interactionTypeForObjective != interactable.interaction.overworldInteraction) return;
        
        var berryTree = interactable.GetComponent<BerryTree>();
        
        berryTree.OnInteractionComplete += CheckEventSuccess;
        return;
        void CheckEventSuccess(bool successful)
        {
            berryTree.OnInteractionComplete -= CheckEventSuccess;
            
            if (!successful) return;
            
            if(berryForObjective.itemName != berryTree.treeData.berryItem.itemName) return;
            
            ClearObjective();
        }
    }
    protected override void OnObjectiveCleared()
    {
        _overworldState.ClearAndLoadNextObjective();
    }
}
