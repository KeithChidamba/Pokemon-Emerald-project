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
    [SerializeField] private ItemObjectiveType itemObjectiveType;
    protected override void OnObjectiveLoaded()
    {
        switch(itemObjectiveType)
        {
            case ItemObjectiveType.EquipItem: SetupItemEquipObjective(); break;
            case ItemObjectiveType.UseItem: SetupItemUsageObjective(); break;
        }
    }
    private void SetupItemEquipObjective()
    {
        if (overworld_actions.Instance.ItemEquipped())
        {
            if (itemForObjective.itemName == overworld_actions.Instance.equippedSpecialItem.itemName)
            {
                InputStateHandler.Instance.ResetGroupUi(InputStateGroup.Bag);
                ClearObjective();
                return;
            }
        } 
        Bag.Instance.OnItemUsed += CheckForItemObjectiveClear;
    }
    private void SetupItemUsageObjective()
    {
        Item_handler.Instance.OnItemUsageSuccessful += CheckIfItemUsed;
    }
    private void CheckIfItemUsed(bool successful)
    {
        var itemUsed = Item_handler.Instance.itemInUse;
        CheckForItemObjectiveClear(itemUsed);
    }
}
