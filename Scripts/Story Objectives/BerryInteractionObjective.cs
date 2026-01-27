using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "berry obj", menuName = "berry interaction objective")]
public class BerryInteractionObjective : InteractionObjective
{
    public Item berryForObjective;
    protected override void InteractionResult(Overworld_interactable interactable)
    {
        if (overworldInteractionForObjective != interactable.overworldInteractionType) return;
        
        var berryTree = interactable.GetComponent<BerryTree>(); 
        
        Action<OverworldInteractionType,bool> checkEventSuccessBerry = 
            (interactionType,success)=> CheckEventSuccess(success,berryTree.treeData.berryItem.itemName);
     
        berryTree.OnInteractionComplete += checkEventSuccessBerry;
        onObjectiveComplete += RemoveSubscription;
        return;
        void RemoveSubscription()
        {
            berryTree.OnInteractionComplete -= checkEventSuccessBerry;
            onObjectiveComplete -= RemoveSubscription;
        } 
    }
    private void CheckEventSuccess(bool successful,string nameCheck)
    {
        if (!successful) return;
        if(berryForObjective.itemName!=nameCheck) return;
      
        onObjectiveComplete?.Invoke();
        ClearObjective();
    }
}
