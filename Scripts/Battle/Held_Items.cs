using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class Held_Items : BattleParticipantModule
{
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private Dialogue_handler _dialogueHandler;
    
    public Held_Items(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _moveUsageHandler = container.Resolve<Move_handler>();
    }
    void DepleteHeldItem(Item heldItem)
    {
        heldItem.quantity = heldItem.isHeldItem? 1 : heldItem.quantity-1; 
    }
    public IEnumerator CheckForUsableItem()
    {
        if (!participant.pokemon.hasItem) yield break;
        
        var heldItem = participant.pokemon.heldItem;
        if (heldItem.quantity == 0 && !heldItem.isHeldItem)
        {//remove consumable held items that are depleted, not ones that just have special functionality
            participant.pokemon.RemoveHeldItem(); yield break; 
        }
        if (!heldItem.canBeUsedInBattle) yield break;
        
        switch (heldItem.itemType)
        {
            case ItemType.Berry:
                yield return DetermineBerryEffect(heldItem);
                break;
            case ItemType.HealHp:
                yield return CheckHealCondition(heldItem);
                break;
            case ItemType.Status:
                yield return CheckStatusCondition(heldItem);
                break;
            //in the future, if there's need to add special held items like focus sash, create new type of item and add it to this switch
        }
        yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
    }
    private IEnumerator DetermineBerryEffect(Item heldItem)
    {
        var berryInfo = heldItem.GetModule<BerryInfoModule>();
        switch (berryInfo.berryType)
        {
            case  Berry.HpHeal:
                yield return CheckHealCondition(heldItem);
                break;
            case  Berry.StatusHeal:
                yield return CheckStatusCondition(heldItem);
                break;
            case  Berry.ConfusionHeal:
                yield return CheckIfConfused();
                break;
        }
    }
    private IEnumerator CheckHealCondition(Item heldItem)
    {
        if(participant.pokemon.hp >= (participant.pokemon.maxHp/2)) yield break;
        
        DepleteHeldItem(heldItem);
        yield return GetHealing(heldItem);
    }    
    private IEnumerator CheckStatusCondition(Item heldItem)
    {
        if(participant.pokemon.statusEffect == StatusEffect.None) yield break;

        DepleteHeldItem(heldItem);
       yield return GetStatusHealing(heldItem);
    }
    private IEnumerator CheckIfConfused()
    {
        if(!participant.isConfused) yield break;

        _dialogueHandler.DisplayDetails(participant.pokemon.pokemonDisplayName+"'s Persim berry healed its confusion");
        participant.isConfused = false;
    }
    private IEnumerator GetHealing(Item heldItem)
    { 
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s "+heldItem.itemName +" healed it");
        var healEffect = heldItem.GetDynamicModule<ItemEffectInfo>().effectValue;
        _moveUsageHandler.HealthGainDisplay(healEffect,healthGainer:participant);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingHealthGain);
    }
    private IEnumerator GetStatusHealing(Item heldItem)
    { 
        Debug.Log("triggered status held item");
        var statusInfo = heldItem.GetModule<StatusHealInfoModule>();
        var curableStatus = statusInfo.statusEffect;
        
        if (curableStatus == StatusEffect.Poison &&
            participant.pokemon.statusEffect == StatusEffect.BadlyPoison)
        {//antidote heals all poison
            curableStatus = StatusEffect.BadlyPoison;
        }
        if (curableStatus != StatusEffect.FullHeal && 
            participant.pokemon.statusEffect != curableStatus)
        { 
            yield break;
        }
        participant.statusHandler.RemoveStatusEffect();
        _battleHandler.RefreshStatusEffectUI();
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s "+heldItem.itemName +" healed it");
    }

}
