using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Linq;

public class Participant_Status : MonoBehaviour
{
    private Battle_Participant _participant;
    private int _statusDuration = 0;
    private int _statusDurationInTurns = 0;
    private bool _healed = false;
    private int _confusionDuration;
    private int trapDuration;
    private readonly Dictionary<PokemonOperations.StatusEffect, Action> _statusEffectMethods = new ();
    public event Action OnStatusCheck;
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
    public void SetupTrapDuration(int numTurns)
    {
        trapDuration = numTurns;
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
    void LooseHp(float percentage)
    {
        _participant.pokemon.TakeDamage( math.ceil(_participant.pokemon.maxHp * percentage) );
    }
    private void StatDrop()
    {
        switch (_participant.pokemon.statusEffect)
        {
            case PokemonOperations.StatusEffect.Burn:
                _participant.pokemon.attack *= 0.5f;
                break;
            case PokemonOperations.StatusEffect.Paralysis:
                _participant.pokemon.speed *= 0.25f;
                break;
        }
    }
    public void CheckStatus()
    {
        OnStatusCheck?.Invoke();
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
        _participant.RefreshStatusEffectImage();
        AssignStatusDamage();
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
    public void CheckTrapDuration(Battle_Participant participant)
    {
        if (_participant != participant) return;
        if (!_participant.isActive) return;
        if (_participant.canEscape) return;
        _participant.canEscape = trapDuration < 1;

        if (trapDuration > 0)
        {
            GetDamageFromStatus(" is hurt by Sand Tomb!", 1 / 16f);
            trapDuration--;
        }
    }
    public void ConfusionCheck(Battle_Participant participant)
    {
        if (_participant != participant) return;
        if (!_participant.isActive) return;
        if (!_participant.isConfused) return;
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
            SetHeal();
        else
            _participant.canAttack = false;
    }

    void RemoveFreezeStatusWithFire(Battle_Participant attacker, Move moveUsed)
    {
        if (moveUsed.type.typeName != "Fire") return;
        RemoveStatusEffect(); //didnt use SetHeal() because i want to only show the message below
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+" was thawed out!");
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
            SetHeal();
        else //wake up early if lucky
        {
            int[] chances = { 25, 33, 50, 100 };
            if (Utility.RandomRange(1, 101) < chances[_statusDuration-1])
                SetHeal();
            else
                _participant.canAttack = false;
            _statusDuration++;
        }
    }
    private void GetDamageFromStatus(string message,float damage)
    {
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+message);
        LooseHp(damage);
    }
    public void NotifyHealing(Battle_Participant participant)
    {
        if (participant != _participant) return;
        if (!_healed) return;
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
    void SetHeal()
    {
        _healed = true;
    }
    void RemoveStatusEffect()
    {
        _participant.pokemon.statusEffect = PokemonOperations.StatusEffect.None;
        _participant.canAttack = true;
        _participant.RefreshStatusEffectImage();
    }
}
