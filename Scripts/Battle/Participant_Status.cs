using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;


public class Participant_Status : MonoBehaviour
{
    private Battle_Participant _participant;
    private int _statusDuration = 0;
    private int _statusDurationInTurns = 0;
    private bool _healed = false;
    private int _statDropImmunityDuration = 0;
    private readonly Dictionary<string, Action> _statusEffectMethods = new ();
    void Start()
    {
        _participant = GetComponent<Battle_Participant>();
        _statusEffectMethods.Add("freeze",freeze_check);
        _statusEffectMethods.Add("sleep",sleep_check);
        _statusEffectMethods.Add("paralysis",paralysis_check);
    }
    public void Get_statusEffect(int numTurns)
    {
        _participant.RefreshStatusEffectImage();
        if (_participant.pokemon.statusEffect == "None") return;
        _statusDuration = 0;
        _statusDurationInTurns = numTurns;
        Stat_drop();
    }

    public void GetStatDropImmunity(int numTurns)
    {
        _statDropImmunityDuration = numTurns;
        _participant.pokemon.immuneToStatReduction = true;
    }
    void loose_HP(float percentage)
    {
        _participant.pokemon.hp -= math.trunc(_participant.pokemon.maxHp * percentage);
    }
    private void Stat_drop()
    {
        switch (_participant.pokemon.statusEffect)
        {
            case "Burn":
                _participant.pokemon.attack *= 0.5f;
                break;
            case "Paralysis":
                _participant.pokemon.speed *= 0.25f;
                break;
        }
    }
    public void Check_status()
    {
        if (overworld_actions.Instance.usingUI) return;
        if (!_participant.isActive) return;
        if(_participant.pokemon.hp<=0 )return;
        if(Battle_handler.Instance.battleOver)return;
        if (_participant.pokemon.isFlinched)
        {
            _participant.pokemon.isFlinched = false;
            _participant.pokemon.canAttack = true;
        }
        if (!_participant.pokemon.canBeDamaged)
            _participant.pokemon.canBeDamaged = true;
        if (_participant.pokemon.statusEffect == "None") return;
        _participant.RefreshStatusEffectImage();
        Status_damage();
    }
    public void StunCheck()
    {
        if (!_participant.isActive) return;
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon !=
            _participant.pokemon) return;
        if (_participant.pokemon.statusEffect == "None") return;
        
        if (_statusEffectMethods.TryGetValue(_participant.pokemon.statusEffect.ToLower(),out Action method))
            method();
        else
            Debug.Log("couldn't find method for status: " + _participant.pokemon.statusEffect.ToLower());
    }
    void Status_damage()
    {
        switch (_participant.pokemon.statusEffect.Replace(" ","").ToLower())
        {
            case "burn":
                Status_Damage_msg(" is hurt by the burn",0.125f);
                break;
            case "poison":
                Status_Damage_msg(" is poisoned", 0.125f);
                break;
            case "badly poison":
                Status_Damage_msg(" is badly poisoned", (1+_statusDuration) / 16f );
                break;
        }
    }
    
    public void CheckStatDropImmunity()
    {
        if (!_participant.pokemon.immuneToStatReduction) return;
        _participant.pokemon.immuneToStatReduction = _statDropImmunityDuration > 0;
        if (_statDropImmunityDuration > 0)
            _statDropImmunityDuration--;
    }
    void freeze_check()
    {
        if (Utility.RandomRange(1, 101) < 10) //10% chance
            SetHeal();
        else
            _participant.pokemon.canAttack = false;
    }
    void paralysis_check()
    {
        if (_participant.pokemon.isFlinched) return;
        //75% chance
        _participant.pokemon.canAttack = Utility.RandomRange(1, 101) < 75;
    }
    void sleep_check()
    {
        if (_statusDuration < 1)//at least sleep for 1 turn
        {
            _participant.pokemon.canAttack = false;
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
                _participant.pokemon.canAttack = false;
            _statusDuration++;
        }
    }
    void Status_Damage_msg(string msg,float damage)
    {
        Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+msg);
        loose_HP(damage);
        _statusDuration++;
        _participant.Invoke(nameof(_participant.CheckIfFainted),0.9f);
    }
    public void NotifyHealing()
    {
        if (!_healed) return;
        switch (_participant.pokemon.statusEffect)
        {
            case "Sleep":
                Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+" Woke UP!");
                break;
            case "Freeze":
                Dialogue_handler.Instance.DisplayBattleInfo(_participant.pokemon.pokemonName+" Unfroze!");
                break;
        }
        remove_status_effect();
        _healed = false;
    }
    void SetHeal()
    {
        _healed = true;
    }
    void remove_status_effect()
    {
        _participant.pokemon.statusEffect = "None";
        _participant.pokemon.canAttack = true;
        _participant.RefreshStatusEffectImage();
    }
}
