using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class HeldItemHandler
{
    private MoveSequenceHandler _moveUsageHandler;
    private DialogueHandler _dialogueHandler;
    private BattleHandler _battleHandler;
    /// <summary>
    /// Stat -> initial stat value -> final stat value
    /// </summary>
    public event Func<Stat,float, float> OnStatModified;
    public BattleParticipant participant;
    
    public HeldItemHandler(ServiceContainer container,BattleParticipant parentParticipant)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        participant = parentParticipant;
    }

    public void SetHeldItemEffect()
    {
        if (!participant.pokemon.hasItem) return;
        var heldItem = participant.pokemon.heldItem;
        if (!heldItem.canBeUsedInBattle) return;
        if (heldItem.isHeldItem)
        {
            ResolveItemLogic(heldItem);
        }
    }
    private void ResolveItemLogic(Item heldItem)
    {
        var type = heldItem.GetDynamicModule<BattleHeldItemTypeInfo>().heldItemType;
        switch (type)
        {
            case BattleHeldItem.ChoiceBand:
            {
                OnStatModified += AccountForChoiceBand;
                _battleHandler.OnSwitchOut += RemoveOnSwitchOut;
                _moveUsageHandler.RefreshStat(Stat.Attack, participant);
                _moveUsageHandler.OnMoveHit += LockFirstSuccessfulMove;
                break;
                void LockFirstSuccessfulMove(BattleParticipant attacker,BattleParticipant victim,Move moveUsed,float finalDamage)
                {
                    if (attacker.participantKey != participant.participantKey) return;
                    _moveUsageHandler.OnMoveHit -= LockFirstSuccessfulMove;
                    participant.currentMoveLock = new MoveLockData(moveUsed);
                }
                void RemoveOnSwitchOut(BattleParticipant switchOutParticipant)
                {
                    if (participant.participantKey != switchOutParticipant.participantKey) return;
                    _battleHandler.OnSwitchOut -= RemoveOnSwitchOut;
                    OnStatModified -= AccountForChoiceBand;
                    participant.currentMoveLock.moveLocked = false;
                    participant.currentMoveLock.moveToLock = null;
                }
                float AccountForChoiceBand(Stat statToModify, float initialStat)
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
    }
    public IEnumerator CheckForConsumableItem()
    {
        if (!participant.pokemon.hasItem) yield break;
        
        var heldItem = participant.pokemon.heldItem;
        if (heldItem.isHeldItem)
        {
            //this method is only for consumables
            yield break;
        }
        
        if (heldItem.quantity == 0)
        {
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
        }
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    public float AccountForStatChange(Stat statToModify,float initialStat)
    {
        return OnStatModified?.Invoke(statToModify, initialStat) ?? initialStat;
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

        heldItem.quantity--;
        yield return GetHealing(heldItem);
    }    
    private IEnumerator CheckStatusCondition(Item heldItem)
    {
        if(participant.pokemon.statusEffect == StatusEffect.None) yield break;

        heldItem.quantity--;
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
