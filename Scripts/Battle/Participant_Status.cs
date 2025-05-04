using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Mathematics;
using UnityEngine;

public class Participant_Status : MonoBehaviour
{
    public Battle_Participant _participant;
    public int status_duration = 0;
    [SerializeField]private int num_status_turns = 0;
    [SerializeField]private bool Healed = false;
    void Start()
    {
        _participant = GetComponent<Battle_Participant>();
    }
    public void Get_statusEffect(int num_turns)
    {
        _participant.refresh_statusIMG();
        if (_participant.pokemon.Status_effect == "None") return;
        status_duration = 0;
        num_status_turns = num_turns;
        Stat_drop();
    }
    void loose_HP(float percentage)
    {
        _participant.pokemon.HP -= math.trunc(_participant.pokemon.max_HP * percentage);
    }
    public void Stat_drop()
    {
        if (_participant.pokemon.Status_effect == "Burn")
            _participant.pokemon.Attack *= 0.5f;
        if (_participant.pokemon.Status_effect == "Paralysis")
            _participant.pokemon.speed *= 0.25f;
    }
    public void Check_status()
    {
        if (overworld_actions.instance.using_ui) return;
        if (!_participant.is_active) return;
        if(_participant.pokemon.HP<=0 )return;
        if(Battle_handler.instance.BattleOver)return;
        if (_participant.pokemon.isFlinched)
        {
            _participant.pokemon.isFlinched = false;
            _participant.pokemon.canAttack = true;
        }
        if (!_participant.pokemon.CanBeDamaged)
            _participant.pokemon.CanBeDamaged = true;
        if (_participant.pokemon.Status_effect == "None") return;
        _participant.refresh_statusIMG();
        Status_damage();
    }
    public void StunCheck()
    {
        if (!_participant.is_active) return;
        if (Battle_handler.instance.Battle_Participants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon !=
            _participant.pokemon) return;
        if (_participant.pokemon.Status_effect == "None") return;
        Invoke(_participant.pokemon.Status_effect.ToLower()+"_check",0f);
    }
    void Status_damage()
    {
        switch (_participant.pokemon.Status_effect.ToLower())
        {
            case "burn":
                Status_Damage_msg(" is hurt by the burn",0.125f);
                break;
            case "poison":
                Status_Damage_msg(" is poisoned", 0.125f);
                break;
            case "badly poison":
                Status_Damage_msg(" is badly poisoned", (1+status_duration) / 16f );
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
        if (Utility.RandomRange(1, 101) < 75)//75% chance
            _participant.pokemon.canAttack = true;
        else
            _participant.pokemon.canAttack = false;
    }
    void sleep_check()
    {
        if (status_duration < 1)//at least sleep for 1 turn
        {
            _participant.pokemon.canAttack = false;
            status_duration++;
            return;
        }
        if (num_status_turns == status_duration)//after 4 turns wake up
            SetHeal();
        else //wake up early if lucky
        {
            int[] chances = { 25, 33, 50, 100 };
            if (Utility.RandomRange(1, 101) < chances[status_duration-1])
                SetHeal();
            else
                _participant.pokemon.canAttack = false;
            status_duration++;
        }
    }
    void Status_Damage_msg(string msg,float damage)
    {
        Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+msg);
        loose_HP(damage);
        status_duration++;
        _participant.Invoke(nameof(_participant.Check_Faint),0.9f);
    }
    public void Notify_Healing()
    {
        if (!Healed) return;
        switch (_participant.pokemon.Status_effect)
        {
            case "Sleep":
                Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" Woke UP!");
                break;
            case "Freeze":
                Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" Unfroze!");
                break;
        }
        remove_status_effect();
        Healed = false;
    }
    void SetHeal()
    {
        Healed = true;
    }
    void remove_status_effect()
    {
        _participant.pokemon.Status_effect = "None";
        _participant.pokemon.canAttack = true;
        _participant.refresh_statusIMG();
    }
}
