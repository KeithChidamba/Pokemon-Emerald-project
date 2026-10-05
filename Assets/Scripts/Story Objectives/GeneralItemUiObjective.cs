using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "general ui obj", menuName = "Objectives/general ui objective")]
public class GeneralItemUiObjective : StoryObjective
{
    public Item itemForObjective;
    
    private enum ItemObjectiveType
    {
        EquipItem,UseItem
    }
    private ItemHandler _itemHandler;
    [SerializeField] private ItemObjectiveType itemObjectiveType;
    
    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        
        _itemHandler = serviceContainer.Resolve<ItemHandler>();
       
        switch(itemObjectiveType)
        {
            case ItemObjectiveType.EquipItem:
            {
                var overworldActions = serviceContainer.Resolve<OverworldActionsHandler>();
                if (overworldActions.ItemEquipped())
                {
                    if (itemForObjective.itemName == overworldActions.equippedSpecialItem.itemName)
                    {
                        ClearObjective();
                        return;
                    }
                } 
                overworldActions.OnItemEquipped += CheckEquip;
                void CheckEquip(Equipable equipable)
                {
                    if (overworldActions.equippedSpecialItem.itemName == itemForObjective.itemName)
                    {
                        overworldActions.OnItemEquipped -= CheckEquip; 
                        ClearObjective();
                    }
                }
                break;
            }
            case ItemObjectiveType.UseItem:
            {
                _itemHandler.OnItemUsed += CheckIfItemUsed;
                void CheckIfItemUsed(Item itemUsed,bool successful)
                {
                    if (!successful) return;
                    if (itemForObjective.itemName == itemUsed.itemName)
                    {
                        _itemHandler.OnItemUsed -= CheckIfItemUsed;
                        ClearObjective();
                    }
                }
                break;
            }
        }
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
