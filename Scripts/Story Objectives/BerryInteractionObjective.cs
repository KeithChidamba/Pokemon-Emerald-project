using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "berry obj", menuName = "Objectives/berry interaction objective")]
public class BerryInteractionObjective : InteractionObjective
{
    public Item berryForObjective;
    protected override void OnObjectiveLoaded()
    {
        dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        overworldStateHandler = serviceContainer.Resolve<OverworldState>();
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        dialogueHandler.OnOptionsDisplayed += CheckInteractionTriggered;
    }
    
    private void CheckInteractionTriggered(Overworld_interactable interactable)
    {
        if (interactionTypeForObjective != interactable.interaction.overworldInteraction) return;
        
       var berryTree = interactable.GetComponent<BerryTree>();
       
        berryTree.OnInteractionComplete += CheckEventSuccess;
        onObjectiveComplete += RemoveSubscription;
        return;

        void CheckEventSuccess(bool successful)
        {
            if (!successful) return;
            
            if(berryForObjective.itemName != berryTree.treeData.berryItem.itemName) return;
            
            onObjectiveComplete?.Invoke();
            ClearObjective();
        }
        void RemoveSubscription()
        {
            berryTree.OnInteractionComplete -= CheckEventSuccess;
            onObjectiveComplete -= RemoveSubscription;
        }  
    }
    protected override void OnObjectiveCleared()
    {
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
