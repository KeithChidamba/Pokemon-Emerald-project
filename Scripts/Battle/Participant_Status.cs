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
        if (_participant.pokemon.Status_effect == "None") return;
        _statusDuration = 0;
        _statusDurationInTurns = numTurns;
        Stat_drop();
    }
    void loose_HP(float percentage)
    {
        _participant.pokemon.HP -= math.trunc(_participant.pokemon.max_HP * percentage);
    }
    private void Stat_drop()
    {
        switch (_participant.pokemon.Status_effect)
        {
            case "Burn":
                _participant.pokemon.Attack *= 0.5f;
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
        if(_participant.pokemon.HP<=0 )return;
        if(Battle_handler.Instance.battleOver)return;
        if (_participant.pokemon.isFlinched)
        {
            _participant.pokemon.isFlinched = false;
            _participant.pokemon.canAttack = true;
        }
        if (!_participant.pokemon.CanBeDamaged)
            _participant.pokemon.CanBeDamaged = true;
        if (_participant.pokemon.Status_effect == "None") return;
        _participant.RefreshStatusEffectImage();
        Status_damage();
    }
    public void StunCheck()
    {
        if (!_participant.isActive) return;
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.Instance.currentTurnIndex].pokemon !=
            _participant.pokemon) return;
        if (_participant.pokemon.Status_effect == "None") return;
        
        if (_statusEffectMethods.TryGetValue(_participant.pokemon.Status_effect.ToLower(),out Action method))
            method();
        else
            Debug.Log("couldn't find method for status: " + _participant.pokemon.Status_effect.ToLower());
    }
    void Status_damage()
    {
        switch (_participant.pokemon.Status_effect.Replace(" ","").ToLower())
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
        Dialogue_handler.Instance.Battle_Info(_participant.pokemon.Pokemon_name+msg);
        loose_HP(damage);
        _statusDuration++;
        _participant.Invoke(nameof(_participant.CheckIfFainted),0.9f);
    }
    public void Notify_Healing()
    {
        if (!_healed) return;
        switch (_participant.pokemon.Status_effect)
        {
            case "Sleep":
                Dialogue_handler.Instance.Battle_Info(_participant.pokemon.Pokemon_name+" Woke UP!");
                break;
            case "Freeze":
                Dialogue_handler.Instance.Battle_Info(_participant.pokemon.Pokemon_name+" Unfroze!");
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
        _participant.pokemon.Status_effect = "None";
        _participant.pokemon.canAttack = true;
        _participant.RefreshStatusEffectImage();
    }
}
