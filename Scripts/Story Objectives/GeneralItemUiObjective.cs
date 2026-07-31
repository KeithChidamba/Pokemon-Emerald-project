using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "general ui obj", menuName = "Objectives/general ui objective")]
public class GeneralItemUiObjective : ItemUiObjective
{
    private enum ItemObjectiveType
    {
        EquipItem,UseItem
    }
    private ItemHandler _itemHandler;
    [SerializeField] private ItemObjectiveType itemObjectiveType;
    
    protected override void LogicForObjectiveLoad()
    {
        switch(itemObjectiveType)
        {
            case ItemObjectiveType.EquipItem: SetupItemEquipObjective(); break;
            case ItemObjectiveType.UseItem: SetupItemUsageObjective(); break;
        }
    }
    private void SetupItemEquipObjective()
    {
        var overworldActions = serviceContainer.Resolve<OverworldActionsHandler>();
        var playerBag = serviceContainer.Resolve<Bag>();
        var inputStateHandler = serviceContainer.Resolve<InputStateHandler>();
        if (overworldActions.ItemEquipped())
        {
            if (itemForObjective.itemName == overworldActions.equippedSpecialItem.itemName)
            {
                inputStateHandler.ResetGroupUi(InputStateGroup.Bag);
                ClearObjective();
                return;
            }
        } 
        playerBag.OnItemUsed += CheckForItemObjectiveClear;
    }
    private void SetupItemUsageObjective()
    {
        _itemHandler = serviceContainer.Resolve<ItemHandler>();
        _itemHandler.OnItemUsed += CheckIfItemUsed;
    }
    private void CheckIfItemUsed(Item itemUsed,bool successful)
    {
        CheckForItemObjectiveClear(itemUsed);
    }
}
