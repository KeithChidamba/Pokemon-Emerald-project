using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ui obj", menuName = "ui objective")]
public class UiActionObjective : StoryObjective
{
    public UiObjectiveType uiUsage;
    public Item itemForObjective;
    public Pokemon pokemonForObjective;
    public override void LoadObjective()
    { 
       Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
       switch(uiUsage)
       {
           case UiObjectiveType.EquipItem: SetupItemEquipObjective(); break;
           
           case UiObjectiveType.UseItem: SetupItemUsageObjective(); break;
           
           case UiObjectiveType.SellItem: SetupItemSellObjective(); break;
           
           case UiObjectiveType.BuyItem: SetupItemBuyObjective(); break;
           
           case UiObjectiveType.WithdrawPokemonFromPC: WithdrawObjective(); break;
           
           case UiObjectiveType.DepositPokemonIntoPC: DepositObjective(); break;
       }
    }
    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
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

    private void CheckForItemObjectiveClear(Item item)
    {
        if (itemForObjective.itemName == item.itemName)
        {
            ClearObjective();
        }
    }

    private void SetupItemSellObjective()
    {
        Bag.Instance.OnItemSold += CheckForItemObjectiveClear;
    }

    private void SetupItemBuyObjective()
    {
        Poke_Mart.Instance.OnItemBought += CheckForItemObjectiveClear;
    }

    private void WithdrawObjective()
    {
        pokemon_storage.Instance.OnPokemonWithdraw += CheckForStorageObjectiveClear;
    }
    private void DepositObjective()
    {
        pokemon_storage.Instance.OnPokemonDeposit += CheckForStorageObjectiveClear;
    }
    private void CheckForStorageObjectiveClear(Pokemon pokemon)
    {
        if (pokemon.pokemonName == pokemonForObjective.pokemonName)
        {
            if(uiUsage == UiObjectiveType.WithdrawPokemonFromPC)
                pokemon_storage.Instance.OnPokemonWithdraw -= CheckForStorageObjectiveClear;
            else pokemon_storage.Instance.OnPokemonDeposit -= CheckForStorageObjectiveClear;
            ClearObjective();
        }
    }
}

public enum UiObjectiveType
{
    EquipItem,UseItem,SellItem,BuyItem,WithdrawPokemonFromPC,DepositPokemonIntoPC
}