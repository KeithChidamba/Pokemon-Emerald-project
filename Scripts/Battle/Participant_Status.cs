using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
[Serializable]
public class Participant_Status : BattleParticipantModule
{
    private int _statusDuration;
    private int _statusDurationInTurns;
    private bool _healed;
    private int _confusionDuration;
    private int _trapDuration;
    private TrapData _currentTrap;
    private readonly Dictionary<StatusEffect, Action> _statusEffectMethods = new ();
    public event Action<Battle_Participant> OnStatusCheck;
    
    private Dialogue_handler _dialogueHandler;
    private Turn_Based_Combat _turnBasedCombatHandler;
    private Battle_handler _battleHandler;
    private Move_handler _moveUsageHandler;
    private overworld_actions _overworldActions;
    private BattleOperations _battleOperationsHandler;
    
    public Participant_Status(ServiceContainer container)
    {
        _battleOperationsHandler = container.Resolve<BattleOperations>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _moveUsageHandler = container.Resolve<Move_handler>();
        _overworldActions = container.Resolve<overworld_actions>();
        OnInject();
    }
    
    private void OnInject()
    {
        _battleHandler.OnBattleEnd += ()=> _moveUsageHandler.OnMoveHit -= RemoveFreezeStatusWithFire;
        
        _statusEffectMethods.Add(StatusEffect.Freeze,FreezeCheck);
        _statusEffectMethods.Add(StatusEffect.Sleep,SleepCheck);
        _statusEffectMethods.Add(StatusEffect.Paralysis,ParalysisCheck);
    }
    public void GetStatusEffect(int numTurns)
    {
        participant.RefreshStatusEffectImage();
        if (participant.pokemon.statusEffect == StatusEffect.None) return;
        if (participant.pokemon.statusEffect == StatusEffect.Freeze)
            _moveUsageHandler.OnMoveHit += RemoveFreezeStatusWithFire;
        
        _statusDuration = 0;
        _statusDurationInTurns = numTurns;
        StatDrop();
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
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName + _currentTrap.OnTrapMessage);
        participant.canEscape = false;
    }
    public void GetStatChangeImmunity(StatChangeability changeability,int numTurns)
    {
        if (participant.statChangeEffects.Any(s => s.Changeability == changeability))
        {
            Debug.Log("added duplicate stat change effect");
        };
        participant.statChangeEffects.Add(new(changeability,numTurns));
    }
    private void StatDrop()
    {
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Burn:
                var atkDrop = new BuffDebuffData(participant, Stat.Attack, false, 2);
                _battleOperationsHandler.canDisplayChange = false; 
                _moveUsageHandler.ExecuteBuffOrDebuff(atkDrop);
                break;
            case StatusEffect.Paralysis:
                var speedDrop = new BuffDebuffData(participant, Stat.Speed, false, 6);
                _battleOperationsHandler.canDisplayChange = false; 
                _moveUsageHandler.ExecuteBuffOrDebuff(speedDrop);
                break;
        }
    }
    public IEnumerator CheckStatus()
    {
        if (_overworldActions.usingUI) yield break; 
        if (!participant.isActive) yield break;
        if(participant.pokemon.hp<=0 )yield break;
        if(_battleHandler.battleOver)yield break;
        
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
        _statusDuration++;
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
                damagePercent = _statusDuration / 16f ;
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
        
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName+message);
        
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
        
        _moveUsageHandler.DisplayDamage(participant,displayEffectiveness:false,isSpecificDamage:true,healthLost,damageSource);
        
        yield return new WaitUntil(() => !_moveUsageHandler.displayingDamage);
       
        participant.pokemon.ChangeHealth(null);  
        
    }
    public void StunCheck()
    {
        if (!participant.isActive) return;
        if (_battleHandler.battleParticipants[_turnBasedCombatHandler.currentTurnIndex].pokemon !=
            participant.pokemon) return;
        if (participant.pokemon.statusEffect == StatusEffect.None) return;
        
        if (_statusEffectMethods.TryGetValue(participant.pokemon.statusEffect,out Action method))
            method();
    }
    public IEnumerator CheckTrapDuration(Battle_Participant participant)
    {
        if (base.participant != participant) yield break;
        if (!base.participant.isActive) yield break;
        if (base.participant.canEscape) yield break;
        if (_currentTrap == null) yield break;
        if (!_currentTrap.hasDuration) yield break;
        if (_trapDuration <= 0)
        {
            _dialogueHandler.DisplayBattleInfo(base.participant.pokemon.pokemonName+_currentTrap.OnFreeMessage);
            RemoveTrap();
            yield break;
        }
        yield return GetDamageFromStatus( 1 / 16f,_currentTrap.OnHitMessage);
        _trapDuration--;
    }
    public IEnumerator ConfusionCheck(Battle_Participant participant)
    {
        if (base.participant != participant) yield break;
        if (!base.participant.isActive) yield break;
        if (!base.participant.isConfused)
        {
            _confusionDuration = 0;
            yield break;
        }
        base.participant.isConfused = _confusionDuration > 0;
        
        if (_confusionDuration > 0) _confusionDuration--;
    }
    public void CheckStatDropImmunity()
    {
        if (!participant.isActive) return;
        if (participant.statChangeEffects.Count==0) return;
        
        participant.statChangeEffects.ForEach(s=>s.EffectDuration--);
        participant.statChangeEffects.RemoveAll(s => s.EffectDuration == 0);
        
    }
    void FreezeCheck()
    {
        if (Utility.RandomRange(1, 101) < 10) //10% chance
            _healed = true;
        else
            participant.canAttack = false;
    }

    void RemoveFreezeStatusWithFire(Battle_Participant attacker, Move moveUsed)
    {
        if (moveUsed.type.typeName != nameof(Types.Fire) ) return;
        RemoveStatusEffect();
        _dialogueHandler.DisplayBattleInfo(participant.pokemon.pokemonName+" was thawed out!");
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
        if (_statusDuration < 1)//at least sleep for 1 turn
        {
            participant.canAttack = false;
            _statusDuration++;
            return;
        }
        if (_statusDurationInTurns == _statusDuration)//after 4 turns wake up
            _healed = true;
        else //wake up early if lucky
        {
            int[] chances = { 25, 33, 50, 100 };
            if (Utility.RandomRange(1, 101) < chances[_statusDuration-1])
                _healed = true;
            else
                participant.canAttack = false;
            _statusDuration++;
        }
    }

    public void RemoveTrap()
    {
        participant.canEscape = true;
        _currentTrap = null;
    }
    public IEnumerator NotifyHealing(Battle_Participant participant)
    {//only for freeze and sleep
        if (participant != base.participant) yield break;
        if (!base.participant.isActive) yield break;
        if (!_healed || base.participant.pokemon.statusEffect==StatusEffect.None) yield break;
        switch (base.participant.pokemon.statusEffect)
        {
            case StatusEffect.Sleep:
                _dialogueHandler.DisplayBattleInfo(base.participant.pokemon.pokemonName+" Woke UP!");
                break;
            case StatusEffect.Freeze:
                _dialogueHandler.DisplayBattleInfo(base.participant.pokemon.pokemonName+" Unfroze!");
                break;
        }
        RemoveStatusEffect();
        _healed = false;
        yield return new WaitUntil(()=> !_dialogueHandler.messagesLoading);
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
        //Remove stat drops caused by status
        switch (participant.pokemon.statusEffect)
        {
            case StatusEffect.Burn:
                var currentAtkBuff =
                    BattleOperations.SearchForBuffOrDebuff(participant.pokemon, Stat.Attack);
                if (currentAtkBuff == null) return;
                BattleOperations.ModifyBuff(currentAtkBuff,0,2);
                participant.pokemon.attack = _moveUsageHandler.ModifyStatValue
                (Stat.Attack, participant.statData.attack, currentAtkBuff.stage);
                break;
            
            case StatusEffect.Paralysis:
                var currenSpdBuff =
                    BattleOperations.SearchForBuffOrDebuff(participant.pokemon, Stat.Speed);
                if (currenSpdBuff == null) return;
                BattleOperations.ModifyBuff(currenSpdBuff,0,6);
                participant.pokemon.speed = _moveUsageHandler.ModifyStatValue
                    (Stat.Speed, participant.statData.speed, currenSpdBuff.stage);
                break;
        }
        BattleOperations.RemoveInvalidBuffsOrDebuffs(participant.pokemon);
        participant.pokemon.statusEffect = StatusEffect.None; 
        participant.RefreshStatusEffectImage();
    }
}
