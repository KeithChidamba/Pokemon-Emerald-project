using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "pickup", menuName = "Objectives/item pickup objective")]
public class ItemPickupObjective : PropBasedObjective
{
    private OverworldState _overworldStateHandler;
    public OverworldPickup itemToPickup;
    
    public override void ReceivePickupObjects(PickupData data)
    {
        if (data.pickup != itemToPickup) return;
        var itemPickupGroup = new propStateGroup(new List<propStateAfterObjective>());
        var pickupObject =  data.GetPickupObject();
        var itemPickState = new propStateAfterObjective(pickupObject,propState.InActive);
        pickupObject.SetActive(false);
        itemPickupGroup.propsForObjective.Add(itemPickState);
        objectiveObjectHandler.propGroupsForObjective.Add(itemPickupGroup);
        _overworldStateHandler.OnPickupItemCreated -= ReceivePickupObjects;
    }

    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _overworldStateHandler.OnItemPickedUp += CheckItem;
        return;

        void CheckItem(Item itemPickedUp)
        {
            if (itemPickedUp.itemName == itemToPickup.item.itemName)
            {
                _overworldStateHandler.OnItemPickedUp -= CheckItem;
                //prevent the clear logic throwing null error because pickups get destroyed after being taken
                objectiveObjectHandler.propGroupsForObjective.Clear();
                ClearObjective();
            }
        }
    }

    protected override void OnObjectiveCleared()
    {
        _overworldStateHandler.ClearAndLoadNextObjective();
    }
}
