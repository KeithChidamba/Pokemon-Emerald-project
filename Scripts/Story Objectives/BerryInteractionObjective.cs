using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "berry obj", menuName = "Objectives/berry interaction objective")]
public class BerryInteractionObjective : InteractionObjective
{
    public Item berryForObjective;
    private string _berryTreeName;
    protected override void OnObjectiveLoaded()
    {
        dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        overworldStateHandler = serviceContainer.Resolve<OverworldState>();
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        dialogueHandler.OnOptionsDisplayed += CheckInteractionTriggered;
    }
    
    private void CheckInteractionTriggered(Overworld_interactable interactable)
    {
        if (interactionForObjective.overworldInteraction != interactable.interaction.overworldInteraction) return;
        
        var berryTree = interactable.GetComponent<BerryTree>();
        _berryTreeName = berryTree.treeData.berryItem.itemName;
     
        berryTree.OnInteractionComplete += CheckEventSuccess;
        onObjectiveComplete += RemoveSubscription;
        return;
        void RemoveSubscription()
        {
            berryTree.OnInteractionComplete -= CheckEventSuccess;
            onObjectiveComplete -= RemoveSubscription;
        } 
        void CheckEventSuccess(bool successful)
        {
            if (!successful) return;
            if(berryForObjective.itemName!=_berryTreeName) return;
      
            onObjectiveComplete?.Invoke();
            ClearObjective();
        }
    }
    protected override void OnObjectiveCleared()
    {
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
