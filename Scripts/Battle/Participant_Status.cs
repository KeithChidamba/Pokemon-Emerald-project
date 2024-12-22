using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Mathematics;
using UnityEngine;

public class Participant_Status : MonoBehaviour
{
    public Battle_Participant _participant;
    public int status_duration = 0;
    private int num_status_turns = 0;
    void Start()
    {
        _participant = GetComponent<Battle_Participant>();
        Turn_Based_Combat.instance.OnNewTurn += Check_status;
    }
    public void Get_statusEffect(int num_turns)
    {
        if (_participant.pokemon.Status_effect == "None") return;
        status_duration = 0;
        num_status_turns = num_turns;
        _participant.refresh_statusIMG();
        Recovery_Chance();
        Stat_drop();
    }
    void loose_HP(float percentage)
    {
        _participant.pokemon.HP -= math.trunc(_participant.pokemon.HP * percentage);
    }
    public void Stat_drop()
    {
        if (_participant.pokemon.Status_effect == "Burn")
            _participant.pokemon.Attack *= 0.5f;
        if (_participant.pokemon.Status_effect == "Paralysis")
            _participant.pokemon.speed *= 0.25f;
    }
    private void Check_status()
    {
        if (!_participant.is_active) return;
        if (_participant.pokemon.isFlinched)
        {
            _participant.pokemon.isFlinched = false;
            _participant.pokemon.canAttack = true;
        }
        if (_participant.pokemon.Status_effect == "None") return;
        _participant.refresh_statusIMG();
        status_duration++;
        Recovery_Chance();
    }
    void Recovery_Chance()
    {
        Invoke("Check_"+_participant.pokemon.Status_effect.ToLower(),0f);
    }
    void Check_burn()
    {
        Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" is hurt by the burn");
        
        loose_HP(0.125f);
    }
    void Check_poison()
    {
        Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" is poisoned");
        
        loose_HP(0.125f);
    }
    void Check_badlypoison()
    {
        Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" is badly poisoned");
        
        loose_HP((status_duration+1)/16f);
    }
    void Check_freeze()
    {
        if (Utility.Get_rand(1, 101) < 10) //10% chance
        {
            Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" Unfroze!");
            
            remove_status_effect();
        }
        else
            _participant.pokemon.canAttack = false;
    }
    void Check_paralysis()
    {
        if (_participant.pokemon.isFlinched) return;
        if (Utility.Get_rand(1, 101) < 75)//75% chance
            _participant.pokemon.canAttack = true;
        else
            _participant.pokemon.canAttack = false;
    }
    void Check_sleep()
    {
        num_status_turns--;
        if (num_status_turns == 0 && status_duration!=0)//at least sleep for 1 turn
            remove_status_effect();
        else
        {
            float[] chances = { 0.25f, 0.33f, 0.5f, 1 };
            Debug.Log("sleep: "+status_duration);
            if (Utility.Get_rand(1, 101) < 100 * chances[status_duration])
            {
                Dialogue_handler.instance.Battle_Info(_participant.pokemon.Pokemon_name+" Woke UP!");
                
                remove_status_effect();
            }
            else
                _participant.pokemon.canAttack = false;
        }
    } 
    void remove_status_effect()
    {
        _participant.pokemon.Status_effect = "None";
        _participant.pokemon.canAttack = true;
        _participant.refresh_statusIMG();
    }
}
