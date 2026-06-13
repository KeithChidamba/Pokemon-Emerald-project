using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class Held_Items : BattleParticipantModule
{
    private Item _heldItem;
    
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private Dialogue_handler _dialogueHandler;
    
    public Held_Items(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _moveUsageHandler = container.Resolve<Move_handler>();
    }
    void DepleteHeldItem()
    {
        _heldItem.quantity = _heldItem.isHeldItem? 1 : _heldItem.quantity-1; 
    }
    public IEnumerator CheckForUsableItem()
    {
        if (!participant.pokemon.hasItem) yield break;
        
        _heldItem = participant.pokemon.heldItem;
        if (_heldItem.quantity == 0 && !_heldItem.isHeldItem)
        {//remove consumable held items that are depleted, not ones that just have special functionality
            participant.pokemon.RemoveHeldItem(); yield break; 
        }
        if (!_heldItem.canBeUsedInBattle) yield break;
        
        switch (_heldItem.itemType)
        {
            case ItemType.Berry:
                yield return DetermineBerryEffect();
                break;
            case ItemType.HealHp:
                yield return CheckHealCondition();
                break;
            case ItemType.Status:
                yield return CheckStatusCondition();
                break;
            //in the future, if there's need to add special held items like focus sash, create new type of item and add it to this switch
        }
        yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
    }
    private IEnumerator DetermineBerryEffect()
    {
        var berryInfo = _heldItem.GetModule<BerryInfoModule>();
        switch (berryInfo.berryType)
        {
            case  Berry.HpHeal:
                yield return CheckHealCondition();
                break;
            case  Berry.StatusHeal:
                yield return CheckStatusCondition();
                break;
            case  Berry.ConfusionHeal:
                yield return CheckIfConfused();
                break;
        }
    }
    private IEnumerator CheckHealCondition()
    {
        if(participant.pokemon.hp >= (participant.pokemon.maxHp/2)) yield break;
        
        DepleteHeldItem();
        yield return GetHealing();
    }    
    private IEnumerator CheckStatusCondition()
    {
        if(participant.pokemon.statusEffect == StatusEffect.None) yield break;

        DepleteHeldItem();
       yield return GetStatusHealing();
    }
    private IEnumerator CheckIfConfused()
    {
        if(!participant.isConfused) yield break;

        _dialogueHandler.DisplayDetails(participant.pokemon.pokemonDisplayName+"'s Persim berry healed its confusion");
        participant.isConfused = false;

    }
    private IEnumerator GetHealing()
    { 
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s "+_heldItem.itemName +" healed it");
        var healEffect = _heldItem.GetDynamicModule<ItemEffectInfo>().effectValue;
        _moveUsageHandler.HealthGainDisplay(healEffect,healthGainer:participant);
        yield return new WaitUntil(() => !_moveUsageHandler.displayingHealthGain);
    }
    private IEnumerator GetStatusHealing()
    { 
        Debug.Log("triggered status held item");
        var statusInfo = _heldItem.GetModule<StatusHealInfoModule>();
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
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+"'s "+_heldItem.itemName +" healed it");
    }

}
