using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
[Serializable]
public class BattleParticipantStatusHandler : BattleParticipantModule
{
    private int _currentStatusTurnCount;
    private int _statusDurationInTurns;
    private bool _healed;
    private int _confusionDuration;
    private int _trapDuration;
    private TrapData _currentTrap;
    private readonly Dictionary<StatusEffect, Action> _statusEffectMethods = new ();
    public event Action<BattleParticipant> OnStatusCheck;
    
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
    }
    
    public void OnInject()
    {
        _battleHandler.OnBattleEnd += ()=> _moveUsageHandler.OnMoveHit -= RemoveFreezeStatusWithFire;
        
        _statusEffectMethods.Add(StatusEffect.Freeze,FreezeCheck);
        _statusEffectMethods.Add(StatusEffect.Sleep,SleepCheck);
        _statusEffectMethods.Add(StatusEffect.Paralysis,ParalysisCheck);
    }
    public void GetStatusEffect(StatusEffect effect,int numTurns)
    {
        participant.pokemon.statusEffect = effect;
        participant.RefreshStatusEffectImage();
        
        _currentStatusTurnCount = 0;
        _statusDurationInTurns = numTurns;
        
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Burn:
                _moveUsageHandler.RefreshStat(Stat.Attack, participant);
                break;

            case StatusEffect.Paralysis:
                _moveUsageHandler.RefreshStat(Stat.Speed, participant);
                break;
            
            case StatusEffect.Freeze:
                _moveUsageHandler.OnMoveHit += RemoveFreezeStatusWithFire;
                break;
        }
    }
    public void GetConfusion(int numTurns)
    {
        _confusionDuration = numTurns;
        participant.isConfused = true;
    }
    public void SetupTrapDuration(int numTurns = 0,Move move = null,bool hasDuration = true)
    {
        if (!hasDuration)
        {
            _currentTrap = new TrapData(null,false);
            participant.canEscape = false;
            return;
        }
        _trapDuration = numTurns;
        _currentTrap = new TrapData(move,true);
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName + _currentTrap.OnTrapMessage);
        participant.canEscape = false;
    }
    public void GetStatChangeImmunity(StatChangeability changeability,int numTurns)
    {
        if (participant.statChangeEffects.Any(s => s.changeability == changeability))
        {
            Debug.Log("added duplicate stat change effect");
        };
        participant.statChangeEffects.Add(new(changeability,numTurns));
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
        
        OnStatusCheck?.Invoke(participant);
        
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
        yield return GetDamageFromStatus(damagePercent, message);
    }

    private IEnumerator GetDamageFromStatus(float damagePercent,string message)
    {        
        var damagingStatuses = new[] { StatusEffect.Poison, StatusEffect.BadlyPoison, StatusEffect.Burn };
        
        if (!damagingStatuses.Contains(participant.pokemon.statusEffect))
        {
            yield break;
        }
        
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
    public void StunCheck()
    {
        if (!participant.isActive) return;
        if (_battleHandler.GetCurrentParticipant().participantKey != participant.participantKey) return;
        if (participant.pokemon.statusEffect == StatusEffect.None) return;
        
        if (_statusEffectMethods.TryGetValue(participant.pokemon.statusEffect,out Action method))
            method();
    }
    public IEnumerator CheckTrapDuration(BattleParticipant currentParticipant)
    {
        if (currentParticipant.participantKey != participant.participantKey) yield break;
        if (!participant.isActive) yield break;
        if (participant.canEscape) yield break;
        if (_currentTrap == null) yield break;
        if (!_currentTrap.hasDuration) yield break;
        if (_trapDuration <= 0)
        {
            _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+_currentTrap.OnFreeMessage);
            RemoveTrap();
            yield break;
        }
        yield return GetDamageFromStatus( 1 / 16f,_currentTrap.OnHitMessage);
        _trapDuration--;
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
    public void CheckStatDropImmunity()
    {
        if (!participant.isActive) return;
        if (participant.statChangeEffects.Count==0) return;
        
        participant.statChangeEffects.ForEach(s=>s.effectDuration--);
        participant.statChangeEffects.RemoveAll(s => s.effectDuration == 0);
        
    }
    void FreezeCheck()
    {
        if (Utility.RandomRange(1, 101) < 10) //10% chance
            _healed = true;
        else
            participant.canAttack = false;
    }

    void RemoveFreezeStatusWithFire(BattleParticipant attacker, Move moveUsed)
    {
        if (moveUsed.type.typeEnum != PokemonType.Fire ) return;
        RemoveStatusEffect();
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonDisplayName+" was thawed out!");
        _healed = true;
        _moveUsageHandler.OnMoveHit -= RemoveFreezeStatusWithFire;
    }
    void ParalysisCheck()
    {
        if (participant.isFlinched) return;
        //75% chance
        participant.canAttack = Utility.RandomRange(1, 101) < 75;
    }
    void SleepCheck()
    {
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
            int[] chances = { 25, 33, 50, 100 };
            if (Utility.RandomRange(1, 101) < chances[_currentStatusTurnCount-1])
                _healed = true;
            else
                participant.canAttack = false;
            _currentStatusTurnCount++;
        }
    }

    public void RemoveTrap()
    {
        participant.canEscape = true;
        _currentTrap = null;
    }
    public IEnumerator NotifyHealing(BattleParticipant currentParticipant)
    {//only for freeze and sleep
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
        _healed = false;
        yield return _dialogueHandler.AwaitAllDialogue();
    }
    public void RemoveStatusEffect(bool healAllEffects = false)
    {
        if (participant.pokemon.statusEffect == StatusEffect.Sleep
            || participant.pokemon.statusEffect == StatusEffect.Paralysis)
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
            case StatusEffect.Burn:
                _moveUsageHandler.RefreshStat(Stat.Attack, participant);
                break;
            case StatusEffect.Paralysis:
                _moveUsageHandler.RefreshStat(Stat.Speed, participant);
                break;
        }
        
        participant.RefreshStatusEffectImage();
    }
}
