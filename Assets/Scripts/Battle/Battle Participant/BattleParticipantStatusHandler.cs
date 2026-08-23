using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;


public enum StatusHandlingState
{
    Normal,Permanent
}
[Serializable]
public class BattleParticipantStatusHandler : BattleParticipantModule
{
    private int _currentStatusTurnCount;
    private int _statusDurationInTurns;
    private bool _healed;
    private int _confusionDuration;
    /// <summary>
    /// [For testing]
    /// </summary>
    private StatusHandlingState _stateControl;
    
    private List<TrapDataInfo> _currentTraps = new();
    public IReadOnlyList<TrapDataInfo> CurrentTraps => _currentTraps;
    
    private readonly Dictionary<StatusEffect, Action> _statusEffectMethods = new ();
    public event Action OnStatusCheck;
    
    private DialogueHandler _dialogueHandler;
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
  
    private BattleOperations _battleOperationsHandler;
    
    public BattleParticipantStatusHandler(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _battleHandler.OnBattleEnd += ()=> _moveUsageHandler.OnMoveHit -= RemoveFreezeStatusWithFire;
        
        _statusEffectMethods.Add(StatusEffect.Freeze,FreezeCheck);
        _statusEffectMethods.Add(StatusEffect.Sleep,SleepCheck);
        _statusEffectMethods.Add(StatusEffect.Paralysis,ParalysisCheck);
        _stateControl = StatusHandlingState.Normal;
    }
    public float AccountForStatChange(Stat statToModify,float initialStat)
    {
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Paralysis:
                if (statToModify == Stat.Speed)
                {
                    return initialStat / 4f;
                }
                break;
        }
        return initialStat;
    }
    public float AccountForStatusInDamage(Move moveUsed,float currentDamage)
    {
        if (participant.pokemon.statusEffect == StatusEffect.Burn
            && !moveUsed.isSpecial 
            && participant.pokemon.ability.abilityName != AbilityName.Guts)
        {
                return Mathf.FloorToInt(currentDamage / 2f);
        }
        return currentDamage;
    }
    public void StunCheck()
    {
        if (!participant.isActive) return;
        if (_battleHandler.GetCurrentParticipant().participantKey != participant.participantKey) return;
        if (participant.pokemon.statusEffect == StatusEffect.None) return;
        
        if (_statusEffectMethods.TryGetValue(participant.pokemon.statusEffect,out Action method))
            method();
    }
    public void ChangeToTestingState(StatusHandlingState state)
    {
        _stateControl = state;
    }
    public void GetStatusEffect(StatusEffect effect,int numTurns)
    {
        participant.pokemon.statusEffect = effect;
        participant.RefreshStatusEffectImage();
        
        _currentStatusTurnCount = 0;
        _statusDurationInTurns = numTurns;
        
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Paralysis:
                _moveUsageHandler.RefreshStat(Stat.Speed, participant);
                ParalysisCheck();
                break;
            case StatusEffect.Freeze:
                _moveUsageHandler.OnMoveHit += RemoveFreezeStatusWithFire;
                FreezeCheck();
                break;
            case StatusEffect.Sleep:
                SleepCheck();
                break;
        }
    }
    public void GetStatChangeImmunity(StatChangeability changeability,int numTurns)
    {
        if (participant.statChangeEffects.Any(s => s.changeability == changeability))
        {
            Debug.Log("added duplicate stat change effect");
        };
        participant.statChangeEffects.Add(new(changeability,numTurns));
    }
    public void CheckStatChangeImmunity()
    {
        if (!participant.isActive) return;
        if (participant.statChangeEffects.Count==0) return;
        
        participant.statChangeEffects.ForEach(s=>s.effectDuration--);
        participant.statChangeEffects.RemoveAll(s => s.effectDuration == 0);
    }
    public IEnumerator CheckStatus()
    {
        if (!participant.isActive) yield break;
        if(participant.pokemon.hp<=0 )yield break;
        if(!_battleHandler.BattleInProgress)yield break;
        
        if (participant.isFlinched)
        {
            participant.isFlinched = false;
            participant.canAttack = true;
        }
        if (!participant.canBeDamaged)
            participant.canBeDamaged = true;
        
        if (participant.pokemon.statusEffect == StatusEffect.None) yield break;
        
        OnStatusCheck?.Invoke();
        
        participant.RefreshStatusEffectImage();
        yield return AssignStatusDamage();
    }
    private IEnumerator AssignStatusDamage()
    {
        _currentStatusTurnCount++;
        string message = "";
        float damagePercent = 0;
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Burn:
                message=" is hurt by the burn";
                damagePercent = 0.125f;
                break;
            case StatusEffect.Poison:
                message=" is poisoned";
                damagePercent = 0.125f;
                break;
            case StatusEffect.BadlyPoison:
                message = " is badly poisoned";
                damagePercent = _currentStatusTurnCount / 16f ;
                break;
        }
        yield return ValidateDamageFromStatus(damagePercent, message);
    }

    private IEnumerator ValidateDamageFromStatus(float damagePercent,string message)
    {
        var damagingStatuses = new[] { StatusEffect.Poison, StatusEffect.BadlyPoison, StatusEffect.Burn };
        
        if (!damagingStatuses.Contains(participant.pokemon.statusEffect))
        {
            yield break;
        }
        yield return GetDamageFromStatus(damagePercent, message);
    }
    private IEnumerator GetDamageFromStatus(float damagePercent,string message)
    {        
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+message);
        
        var damageSource = DamageSource.Normal;
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Poison:
            case StatusEffect.BadlyPoison:
                damageSource = DamageSource.Poison;
                break;
            case StatusEffect.Burn:
                damageSource = DamageSource.Burn;
                break;
        }
        var healthLost = math.ceil(participant.pokemon.maxHp * damagePercent);
        
        _moveUsageHandler.DisplaySpecialDamage(participant,predefinedDamage:healthLost,damageSource);
        
        yield return _moveUsageHandler.AwaitDamageDisplay();
       
        participant.pokemon.NotifyHealthChange();  
    }

    public void SetupTrapDuration(TrapDataInfo trapData,bool displayMessage = true)
    {
        var existingTrap = _currentTraps.FirstOrDefault(trap => trap.trapType == trapData.trapType);
        if (existingTrap != null)
        {
            _currentTraps.Remove(existingTrap);
        }
        _currentTraps.Add(trapData);
        participant.canEscape = false;
        
        if (!displayMessage) return;
        
        var isPersistent = trapData.trapType == TrapDataInfo.TrapType.PersistentFromMove;

        _dialogueHandler.DisplayBattleInfo(
            participant.pokemon.pokemonDisplayName
            + (isPersistent? " can’t escape!" 
                : trapData.onTrapMessage));
    }
    public void RemoveTrap(TrapDataInfo.TrapType type)
    {
        _currentTraps.RemoveAll(trap=>trap.trapType == type);
        participant.canEscape = _currentTraps.Count == 0;
    }
    public IEnumerator CheckTrapDuration(BattleParticipant currentParticipant)
    {
        if (currentParticipant.participantKey != participant.participantKey) yield break;
        if (!participant.isActive) yield break;
        if (participant.canEscape) yield break;
        if (_currentTraps.Count == 0) yield break;
        
        var existingTrapWithDuration =
            _currentTraps.FirstOrDefault(trap => 
                trap.trapType == TrapDataInfo.TrapType.RandomDurationFromMove);
        
        if (existingTrapWithDuration != null)
        {
            if (existingTrapWithDuration.trapDuration <= 0)
            {
                _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName + existingTrapWithDuration.onFreeMessage);
                RemoveTrap(existingTrapWithDuration.trapType);
                yield break;
            }
            existingTrapWithDuration.trapDuration--;
            yield return GetDamageFromStatus(1 / 16f, existingTrapWithDuration.onHitMessage);
        }
    }
    public void GetConfusion(int numTurns)
    {
        _confusionDuration = numTurns;
        participant.isConfused = true;
    }
    public IEnumerator ConfusionCheck(BattleParticipant currentParticipant)
    {
        if (currentParticipant != participant) yield break;
        if (!participant.isActive) yield break;
        if (!participant.isConfused)
        {
            _confusionDuration = 0;
            yield break;
        }
        participant.isConfused = _confusionDuration > 0;
        
        if (_confusionDuration > 0) _confusionDuration--;
    }

    private void FreezeCheck()
    {
        if (_stateControl == StatusHandlingState.Permanent)
        {
            participant.canAttack = false;
            return;
        }
        if (Utility.RandomChance(CommonRandom.Rnd10))
            _healed = true;
        else
            participant.canAttack = false;
    }
    private void RemoveFreezeStatusWithFire(BattleParticipant attacker,BattleParticipant victim, Move moveUsed,float finalDamage)
    {
        if (victim.participantKey != participant.participantKey) return;
        if (moveUsed.type.typeEnum != PokemonType.Fire) return;
        _moveUsageHandler.OnMoveHit -= RemoveFreezeStatusWithFire;
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+" was thawed out!");
        RemoveStatusEffect();
    }
    private void ParalysisCheck()
    {
        if (_stateControl == StatusHandlingState.Permanent)
        {
            participant.canAttack = false;
            return;
        }
        if (participant.isFlinched) return;
        participant.canAttack = Utility.RandomChance(CommonRandom.Rnd75);
    }
    private void SleepCheck()
    {
        if (_stateControl == StatusHandlingState.Permanent)
        {
            participant.canAttack = false;
            return;
        }
        if (_currentStatusTurnCount < 1)//at least sleep for 1 turn
        {
            participant.canAttack = false;
            _currentStatusTurnCount++;
            return;
        }
        if (_statusDurationInTurns == _currentStatusTurnCount)//after 4 turns wake up
            _healed = true;
        else //wake up early if lucky
        {
            CommonRandom[] chances = { CommonRandom.Rnd25, CommonRandom.Rnd33, CommonRandom.Rnd50, CommonRandom.Rnd100 };
            if (Utility.RandomChance(chances[_currentStatusTurnCount-1]))
                _healed = true;
            else
                participant.canAttack = false;
            _currentStatusTurnCount++;
        }
    }
    public IEnumerator NotifyHealing(BattleParticipant currentParticipant)
    {
        if (currentParticipant.participantKey != participant.participantKey) yield break;
        if (!participant.isActive) yield break;
        if (!_healed || participant.pokemon.statusEffect==StatusEffect.None) yield break;
        
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Sleep:
                _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+" Woke UP!");
                break;
            case StatusEffect.Freeze:
                _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+" Unfroze!");
                break;
        }
        RemoveStatusEffect();
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    public void RemoveStatusEffect(bool healAllEffects = false)
    {
        _healed = false;
        if (participant.pokemon.statusEffect == StatusEffect.Sleep)
        {
            _statusDurationInTurns = 0;
            _currentStatusTurnCount = 0;
            participant.canAttack = true;
        }
        if (participant.pokemon.statusEffect == StatusEffect.Paralysis)
        {
            participant.canAttack = true;
        }
        if(participant.pokemon.statusEffect == StatusEffect.Freeze)
        {
            _moveUsageHandler.OnMoveHit -= RemoveFreezeStatusWithFire;
            participant.canAttack = true;
        }
        if (healAllEffects)
        {
            participant.isConfused = false;
        }
        StatusEffect previousStatus = participant.pokemon.statusEffect;
        
        participant.pokemon.statusEffect = StatusEffect.None; 
        
        switch (previousStatus)
        {
            case StatusEffect.Paralysis:
                _moveUsageHandler.RefreshStat(Stat.Speed, participant);
                break;
        }
        
        participant.RefreshStatusEffectImage();
    }
}
