using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "pickup", menuName = "Objectives/item pickup objective")]
public class ItemPickupObjective : StoryObjective
{
    public OverworldPickup itemToPickup;
    private OverworldState _overworldStateHandler;
    
    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);

        _overworldStateHandler.AddPickup(itemToPickup);
        
        _overworldStateHandler.OnItemPickedUp += CheckItem;
        return;
        void CheckItem(Item itemPickedUp)
        {
            if (itemPickedUp.itemName == itemToPickup.item.itemName)
            {
                _overworldStateHandler.OnItemPickedUp -= CheckItem;
                ClearObjective();
            }
        }
    }

    protected override void OnObjectiveCleared()
    { 
        _overworldStateHandler.ClearAndLoadNextObjective();
    }
}
