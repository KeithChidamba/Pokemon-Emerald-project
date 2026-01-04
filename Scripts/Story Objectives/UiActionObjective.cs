using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ui obj", menuName = "ui objective")]
public class UiActionObjective : StoryObjective
{
    public UiObjectiveType uiUsage;
    public Item itemForObjective;
    public override void LoadObjective()
    { 
       Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
       switch(uiUsage)
       {
           case UiObjectiveType.EquipItem: SetupItemEquipObjective(); break;
           
           case UiObjectiveType.UseItem: SetupItemUsageObjective(); break;
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
        Bag.Instance.OnItemUsed += CheckEquipped;
    }
    private void SetupItemUsageObjective()
    {
        Item_handler.Instance.OnItemUsageSuccessful += CheckIfItemUsed;
    }
    private void CheckEquipped(Item selectedItem)
    {
        if (itemForObjective.itemName == selectedItem.itemName)
        {
            InputStateHandler.Instance.ResetGroupUi(InputStateGroup.Bag);
            InputStateHandler.Instance.ResetRelevantUi(InputStateName.PlayerMenu);
            ClearObjective();
        }
    }
    private void CheckIfItemUsed(bool successful)
    {
        var itemUsed = Item_handler.Instance.itemInUse;
        if (itemForObjective.itemName == itemUsed.itemName)
        {
            InputStateHandler.Instance.ResetGroupUi(InputStateGroup.Bag);
            InputStateHandler.Instance.ResetRelevantUi(InputStateName.PlayerMenu);
            ClearObjective();
        }
    }
    public override void ClearObjective()
    {
        Dialogue_handler.Instance.RemoveObjectiveText();
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}

public enum UiObjectiveType
{
    EquipItem,UseItem,SellItem,BuyItem,
    SwapPokemon,WithdrawPokemonFromPC,DepositPokemonIntoPC
}