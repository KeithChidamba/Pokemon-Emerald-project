using System;
using System.Collections;
using UnityEngine;

public enum HeldItemEffectExecution
{
    BeforeMoveExecution,
    AfterMoveExecution,
    OnTurnsComplete
}
[Serializable]
public class HeldItemHandler : BattleParticipantModule
{
    private MoveSequenceHandler _moveUsageHandler;
    private DialogueHandler _dialogueHandler;
    private BattleHandler _battleHandler;
    /// <summary>
    /// Stat -> initial stat value -> final stat value
    /// </summary>
    public event Func<Stat,float, float> OnStatModified;

    private bool _itemEffectActivated;
    
    public HeldItemHandler(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        
        
    }

    void DepleteHeldItem(Item heldItem)
    {
        heldItem.quantity = heldItem.isHeldItem? 1 : heldItem.quantity-1; 
    }
    public IEnumerator CheckForUsableItem(HeldItemEffectExecution effectExecution)
    {
        if (!participant.pokemon.hasItem) yield break;
        
        var heldItem = participant.pokemon.heldItem;
        if (heldItem.quantity == 0 && !heldItem.isHeldItem)
        {
            //remove consumable held items that are depleted, not ones that just have special functionality
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
            case ItemType.HeldItem:
                yield return ResolveItemLogic(heldItem);
                break;
        }
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    public float AccountForStatChange(Stat statToModify,float initialStat)
    {
        return OnStatModified?.Invoke(statToModify, initialStat) ?? initialStat;
    }
    private IEnumerator ResolveItemLogic(Item heldItem)
    {
        var type = heldItem.GetDynamicModule<BattleHeldItemTypeInfo>().heldItemType;
        switch (type)
        {
            case BattleHeldItem.ChoiceBand:
            {
                OnStatModified += AccountForChoiceBand;
                _battleHandler.OnSwitchOut += RemoveOnSwitchOut;
                _moveUsageHandler.RefreshStat(Stat.Attack, participant);
                break;
                void RemoveOnSwitchOut(BattleParticipant switchOutParticipant)
                {
                    if (participant.participantKey != switchOutParticipant.participantKey) return;
                    _battleHandler.OnSwitchOut -= RemoveOnSwitchOut;
                    OnStatModified -= AccountForChoiceBand;
                }
                float AccountForChoiceBand(Stat statToModify,float initialStat)
                {
                    if (statToModify == Stat.Attack)
                    {
                        if (participant.pokemon.statusEffect != StatusEffect.None)
                        {
                            return initialStat * 1.5f;
                        }
                    }
                    return initialStat;
                }
            }
        }
        yield return null;
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
                yield return CheckIfConfused(heldItem);
                break;
        }
    }
    private IEnumerator CheckHealCondition(Item heldItem)
    {
        if(participant.pokemon.hp >= participant.pokemon.maxHp/2f) yield break;
        
        DepleteHeldItem(heldItem);
        yield return GetHealing(heldItem);
    }    
    private IEnumerator CheckStatusCondition(Item heldItem)
    {
        if(participant.pokemon.statusEffect == StatusEffect.None) yield break;

        DepleteHeldItem(heldItem);
       yield return GetStatusHealing(heldItem);
    }
    private IEnumerator CheckIfConfused(Item heldItem)
    {
        if(!participant.isConfused) yield break;

        _dialogueHandler.DisplayBattleInfo($"{participant.pokemon.pokemonDisplayName}'s {heldItem.itemName} healed its confusion");
        participant.isConfused = false;
    }
    private IEnumerator GetHealing(Item heldItem)
    { 
        _dialogueHandler.DisplayBattleInfo($"{participant.pokemon.pokemonDisplayName}'s {heldItem.itemName} healed it");
        var healEffect = heldItem.GetDynamicModule<ItemEffectInfo>().effectValue;
        _moveUsageHandler.HealthGainDisplay(healEffect,healthGainer:participant);
        yield return _moveUsageHandler.AwaitHealthGainDisplay();
    }
    private IEnumerator GetStatusHealing(Item heldItem)
    {
        StatusEffect curableStatus;
        
        if (heldItem.itemType == ItemType.Berry)
        {
            var berryInfo = heldItem.GetModule<BerryInfoModule>();
            curableStatus = berryInfo.statusEffect;
        }
        else
        {
            var statusInfo = heldItem.GetModule<StatusHealInfoModule>();
            curableStatus = statusInfo.statusEffect;
        }
        
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
        participant.statusHandler.RemoveStatusEffect(curableStatus == StatusEffect.FullHeal);
        participant.RefreshStatusEffectImage();
        _dialogueHandler.DisplayBattleInfo($"{participant.pokemon.pokemonDisplayName}'s {heldItem.itemName} healed it");
    }

}
