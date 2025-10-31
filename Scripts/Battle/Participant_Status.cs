using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;

public class Participant_Status : MonoBehaviour
{
    private Battle_Participant _participant;
    private int _statusDuration = 0;
    private int _statusDurationInTurns = 0;
    private bool _healed = false;
    private int _confusionDuration;
    private int _trapDuration;
    [SerializeField]private TrapData _currentTrap;
    private readonly Dictionary<PokemonOperations.StatusEffect, Action> _statusEffectMethods = new ();
    public event Action<Battle_Participant> OnStatusCheck;
    public bool dealingStatusDamage;
    void Start()
    {
        _participant = GetComponent<Battle_Participant>();
        _statusEffectMethods.Add(PokemonOperations.StatusEffect.Freeze,FreezeCheck);
        _statusEffectMethods.Add(PokemonOperations.StatusEffect.Sleep,SleepCheck);
        _statusEffectMethods.Add(PokemonOperations.StatusEffect.Paralysis,ParalysisCheck);
        Battle_handler.Instance.OnBattleEnd += ()=> Move_handler.Instance.OnMoveHit -= RemoveFreezeStatusWithFire;
    }
    public void GetStatusEffect(int numTurns)
    {
        _participant.RefreshStatusEffectImage();
        if (_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.None) return;
        if (_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Freeze)
            Move_handler.Instance.OnMoveHit += RemoveFreezeStatusWithFire;
        
        _statusDuration = 0;
        _statusDurationInTurns = numTurns;
        StatDrop();
    }
    public void GetConfusion(int numTurns)
    {
        _confusionDuration = numTurns;
        _participant.isConfused = true;
    }
    public void SetupTrapDuration(int numTurns = 0,Move move = null,bool hasDuration = true)
    {
        if (!hasDuration)
        {
            _currentTrap = new TrapData(null,false);
            _participant.canEscape = false;
            return;
        }
        _trapDuration = numTurns;
        _currentTrap = new TrapData(move,true);
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName + _currentTrap.OnTrapMessage);
        _participant.canEscape = false;
    }
    public void GetStatChangeImmunity(StatChangeData.StatChangeability changeability,int numTurns)
    {
        if (_participant.StatChangeEffects.Any(s => s.Changeability == changeability))
        {
            Debug.Log("added duplicate stat change effect");
        };
        _participant.StatChangeEffects.Add(new(changeability,numTurns));
    }
    private void StatDrop()
    {
        switch (_participant.pokemon.statusEffect)
        {
            case PokemonOperations.StatusEffect.Burn:
                var atkDrop = new BuffDebuffData(_participant, PokemonOperations.Stat.Attack, false, 2);
                BattleOperations.CanDisplayDialougue = false; 
                Move_handler.Instance.SelectRelevantBuffOrDebuff(atkDrop);
                break;
            case PokemonOperations.StatusEffect.Paralysis:
                var speedDrop = new BuffDebuffData(_participant, PokemonOperations.Stat.Speed, false, 6);
                BattleOperations.CanDisplayDialougue = false; 
                Move_handler.Instance.SelectRelevantBuffOrDebuff(speedDrop);
                break;
        }
    }
    public void CheckStatus()
    {
        if (overworld_actions.Instance.usingUI) return; 
        if (!_participant.isActive) return;
        if(_participant.pokemon.hp<=0 )return;
        if(Battle_handler.Instance.battleOver)return;
        
        if (_participant.isFlinched)
        {
            _participant.isFlinched = false;
            _participant.canAttack = true;
        }
        if (!_participant.canBeDamaged)
            _participant.canBeDamaged = true;
        
        if (_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.None) return;
        
        OnStatusCheck?.Invoke(_participant);
        _participant.RefreshStatusEffectImage();
        AssignStatusDamage();
    }
    private void GetDamageFromStatus(string message,float damage)
    {
        dealingStatusDamage = true;
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+message);
        StartCoroutine(LooseHp(damage));
    }
    private IEnumerator LooseHp(float percentage)
    {
        var damageSource = Move_handler.DamageSource.Normal;
        switch (_participant.pokemon.statusEffect)
        {
            case PokemonOperations.StatusEffect.Poison:
            case PokemonOperations.StatusEffect.BadlyPoison:
                damageSource = Move_handler.DamageSource.Poison;
                break;
            case PokemonOperations.StatusEffect.Burn:
                damageSource = Move_handler.DamageSource.Burn;
                break;
        }
       
        Move_handler.Instance.DisplayDamage(_participant,displayEffectiveness:false
            ,isSpecificDamage:true,math.ceil(_participant.pokemon.maxHp * percentage),damageSource);
        
        yield return new WaitUntil(() => !Move_handler.Instance.displayingDamage);
        dealingStatusDamage = false;
        _participant.pokemon.ChangeHealth(null);  
    }
    private void AssignStatusDamage()
    {
        _statusDuration++;
        switch (_participant.pokemon.statusEffect)
        {
            case PokemonOperations.StatusEffect.Burn:
                GetDamageFromStatus(" is hurt by the burn",0.125f);
                break;
            case PokemonOperations.StatusEffect.Poison:
                GetDamageFromStatus(" is poisoned", 0.125f);
                break;
            case PokemonOperations.StatusEffect.BadlyPoison:
                GetDamageFromStatus(" is badly poisoned", (_statusDuration) / 16f );
                break;
        }
    }
    public void StunCheck()
    {
        if (!_participant.isActive) return;
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon !=
            _participant.pokemon) return;
        if (_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.None) return;
        
        if (_statusEffectMethods.TryGetValue(_participant.pokemon.statusEffect,out Action method))
            method();
    }
    public void CheckTrapDuration(Battle_Participant participant)
    {
        if (_participant != participant) return;
        if (!_participant.isActive) return;
        if (_participant.canEscape) return;
        if (_currentTrap == null) return;
        if (!_currentTrap.hasDuration) return;
        if (_trapDuration <= 0)
        {
            Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+_currentTrap.OnFreeMessage);
            RemoveTrap();
            return;
        }
        GetDamageFromStatus(_currentTrap.OnHitMessage, 1 / 16f);
        _trapDuration--;
    }
    public void ConfusionCheck(Battle_Participant participant)
    {
        if (_participant != participant) return;
        if (!_participant.isActive) return;
        if (!_participant.isConfused)
        {
            _confusionDuration = 0;
            return;
        }
        _participant.isConfused = _confusionDuration > 0;
        
        if (_confusionDuration > 0) _confusionDuration--;
    }
    public void CheckStatDropImmunity()
    {
        if (!_participant.isActive) return;
        if (_participant.StatChangeEffects.Count==0) return;
        
        _participant.StatChangeEffects.ForEach(s=>s.EffectDuration--);
        _participant.StatChangeEffects.RemoveAll(s => s.EffectDuration == 0);
        
    }
    void FreezeCheck()
    {
        if (Utility.RandomRange(1, 101) < 10) //10% chance
            _healed = true;
        else
            _participant.canAttack = false;
    }

    void RemoveFreezeStatusWithFire(Battle_Participant attacker, Move moveUsed)
    {
        if (moveUsed.type.typeName != nameof(PokemonOperations.Types.Fire) ) return;
        RemoveStatusEffect();
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+" was thawed out!");
        _healed = true;
        Move_handler.Instance.OnMoveHit -= RemoveFreezeStatusWithFire;
    }
    void ParalysisCheck()
    {
        if (_participant.isFlinched) return;
        //75% chance
        _participant.canAttack = Utility.RandomRange(1, 101) < 75;
    }
    void SleepCheck()
    {
        if (_statusDuration < 1)//at least sleep for 1 turn
        {
            _participant.canAttack = false;
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
                _participant.canAttack = false;
            _statusDuration++;
        }
    }

    public void RemoveTrap()
    {
        _participant.canEscape = true;
        _currentTrap = null;
    }
    public void NotifyHealing(Battle_Participant participant)
    {//only for freeze and sleep
        if (participant != _participant) return;
        if (!_participant.isActive) return;
        if (!_healed || _participant.pokemon.statusEffect==PokemonOperations.StatusEffect.None) return;
        switch (_participant.pokemon.statusEffect)
        {
            case PokemonOperations.StatusEffect.Sleep:
                Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+" Woke UP!");
                break;
            case PokemonOperations.StatusEffect.Freeze:
                Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+" Unfroze!");
                break;
        }
        RemoveStatusEffect();
        _healed = false;
    }
    public void RemoveStatusEffect(bool healAllEffects = false)
    {
        if (_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Sleep
            || _participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Paralysis)
        {
                _participant.canAttack = true;
        }
        if(_participant.pokemon.statusEffect == PokemonOperations.StatusEffect.Freeze)
        {
            Move_handler.Instance.OnMoveHit -= RemoveFreezeStatusWithFire;
            _participant.canAttack = true;
        }
        if (healAllEffects)
        {
            _participant.isConfused = false;
        }
        //Remove stat drops caused by status
        switch (_participant.pokemon.statusEffect)
        {
            case PokemonOperations.StatusEffect.Burn:
                var currentAtkBuff =
                    BattleOperations.SearchForBuffOrDebuff(_participant.pokemon, PokemonOperations.Stat.Attack);
                if (currentAtkBuff == null) return;
                BattleOperations.ModifyBuff(currentAtkBuff,0,2);
                _participant.pokemon.attack = Move_handler.Instance.ModifyStatValue
                (PokemonOperations.Stat.Attack, _participant.statData.attack, currentAtkBuff.stage);
                break;
            
            case PokemonOperations.StatusEffect.Paralysis:
                var currenSpdBuff =
                    BattleOperations.SearchForBuffOrDebuff(_participant.pokemon, PokemonOperations.Stat.Speed);
                if (currenSpdBuff == null) return;
                BattleOperations.ModifyBuff(currenSpdBuff,0,6);
                _participant.pokemon.speed = Move_handler.Instance.ModifyStatValue
                    (PokemonOperations.Stat.Speed, _participant.statData.speed, currenSpdBuff.stage);
                break;
        }
        BattleOperations.RemoveInvalidBuffsOrDebuffs(_participant.pokemon);
        _participant.pokemon.statusEffect = PokemonOperations.StatusEffect.None; 
        _participant.RefreshStatusEffectImage();
    }
}
