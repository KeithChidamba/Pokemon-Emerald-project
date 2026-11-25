using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "general ui obj", menuName = "Objectives/general ui objective")]
public class GeneralItemUiObjective : StoryObjective
{
    public Item itemForObjective;

    private enum ItemObjectiveType
    {
        EquipItem,
        UseItem
    }

    private ItemHandler _itemHandler;
    private PlayerBagHandler _playerBag;
    private OverworldActionsHandler _overworldActions;
    
    [SerializeField] private ItemObjectiveType itemObjectiveType;

    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<DialogueHandler>();
        dialogueHandler.DisplayObjectiveText(objectiveHeading);

        _itemHandler = serviceContainer.Resolve<ItemHandler>();
        _playerBag = serviceContainer.Resolve<PlayerBagHandler>();
        _overworldActions = serviceContainer.Resolve<OverworldActionsHandler>();
        
        switch (itemObjectiveType)
        {
            case ItemObjectiveType.EquipItem:
            {
                if (_overworldActions.ItemEquipped())
                {
                    if (itemForObjective.itemName == _overworldActions.equippedSpecialItem.itemName)
                    {
                        ClearObjective();
                        return;
                    }
                }
                _itemHandler.equipRestriction.SetRestriction(itemForObjective);
                
                _overworldActions.OnItemEquipped += CheckEquip;

                void CheckEquip(Equipable equipable)
                {
                    if (_overworldActions.equippedSpecialItem.itemName == itemForObjective.itemName)
                    {
                        _overworldActions.OnItemEquipped -= CheckEquip;
                        ClearObjective();
                    }
                }

                break;
            }
            case ItemObjectiveType.UseItem:
            {
                _itemHandler.OnItemUsed += CheckIfItemUsed;
                
                _playerBag.itemUsageRestriction.SetRestriction(itemForObjective);
                
                void CheckIfItemUsed(Item itemUsed, bool successful)
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
        _playerBag.itemUsageRestriction.RemoveRestriction();
        _itemHandler.equipRestriction.RemoveRestriction();
        
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>();
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
