using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour
{
    public bool Doing_move = false;
    public static Move_handler instance;
    private Turn current_turn;
    private readonly float[] Stat_Levels = {0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] Accuracy_Evasion_Levels = {0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] Crit_Levels = {6.25f,12.5f,25f,50f};
    private Battle_event[] Dialouge_order={null,null,null,null,null};
    public event Action OnTurnEnd;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        Battle_handler.instance.onBattleEnd += StopAllCoroutines;
    }

    public void Do_move(Turn turn)
    {
        current_turn=turn;
        Dialogue_handler.instance.Battle_Info(turn.attacker_.pokemon.Pokemon_name+" used "+turn.move_.Move_name+" on "+turn.victim_.pokemon.Pokemon_name+"!");
        StartCoroutine(Move_Sequence());
        //Invoke(nameof(PlayAnimation),1f);
    }
    void PlayAnimation()
    {
        //call anim script and run move_name as anim
        //Move_effect in anim event
    }
    void Set_Sequences()
    {
        Dialouge_order[0] = new("Deal_Damage", current_turn.move_.Move_damage > 0,2.5f);
        Dialouge_order[1] = new("Get_status", current_turn.move_.Has_status,1.5f);
        Dialouge_order[2] = new("Move_effect", current_turn.move_.Has_effect,2f);
        Dialouge_order[3] = new("Set_buff_Debuff", current_turn.move_.is_Buff_Debuff,1.5f);
        Dialouge_order[4] = new("flinch_enemy", current_turn.move_.Can_flinch,1f);
    }
    IEnumerator Move_Sequence()//anim event
    {
        Set_Sequences();
        foreach (Battle_event d in Dialouge_order)
        {
            if (d.Condition)
            {
                d.Execute();
                yield return new WaitForSeconds(d.duration);
            }
            else
            {
                d.Execute();
            }
        }
        Move_done();
        yield return null;
    }

    void Move_effect()
    {
        if (!current_turn.move_.Has_effect) return;
        Invoke(current_turn.move_.Move_name.ToLower(), 0f);
    }
    float Calc_Damage()
    {
        if (current_turn.move_.Move_damage == 0) return 0f;
        if (!current_turn.victim_.pokemon.CanBeDamaged)
        {
            Dialogue_handler.instance.Battle_Info(current_turn.victim_.pokemon.Pokemon_name+" protected itself");
            return 0f;
        }
        float damage_dealt;
        float level_factor = (float)((current_turn.attacker_.pokemon.Current_level * 2) / 5) + 2;
        float Stab = 1f;
        int crit = 1;
        float Attack_type = 0;
        float atk_def_ratio;
        float random_factor = (float)Utility.Get_rand(85, 101) / 100;
        float type_effectiveness = BattleOperations.TypeEffectiveness(current_turn.victim_.pokemon, current_turn.move_.type);
        if (current_turn.move_.isSpecial)
        {
            Attack_type = current_turn.attacker_.pokemon.SP_ATK;
            atk_def_ratio = Attack_type / current_turn.victim_.pokemon.SP_DEF;
        }
        else
        {
            Attack_type = current_turn.attacker_.pokemon.Attack;
            atk_def_ratio = Attack_type / current_turn.victim_.pokemon.Defense;
        }
        if (BattleOperations.is_Stab(current_turn.attacker_.pokemon, current_turn.move_.type))
            Stab = 1.5f;
        if (Utility.Get_rand(1, (int)(100 / current_turn.attacker_.pokemon.crit_chance) + 1) < 2)
        {
            crit = 2;
            Dialogue_handler.instance.Battle_Info("Critical Hit!");
        }
        float base_Damage = (level_factor * current_turn.move_.Move_damage *
                             (Attack_type/ current_turn.move_.Move_damage))/current_turn.attacker_.pokemon.Current_level;
        float Modifier = crit*Stab*random_factor*type_effectiveness;
        damage_dealt = math.trunc(Modifier * base_Damage * atk_def_ratio);
        if (type_effectiveness > 1)
            Dialogue_handler.instance.Battle_Info("The move is Super effective!");
        return damage_dealt;
    }
    void Deal_Damage()//anim event
    {
        if (current_turn.move_.Has_effect) return;
        current_turn.victim_.pokemon.HP -= Calc_Damage();
    }
    public void Move_done()
    {
        Doing_move = false;
        OnTurnEnd?.Invoke();
    }
    void Get_status()
    {
        if (!current_turn.move_.Has_status)return;
        if (current_turn.victim_.pokemon.Status_effect != "None") return;
        if (Utility.Get_rand(1, 101) < current_turn.move_.Status_chance)
            Set_Status(current_turn.victim_,current_turn.move_.Status_effect);
        else
        {
            Debug.Log("status failed");
        }
    }
    public void Set_Status(Battle_Participant p,String Status)
    {
        p.pokemon.Status_effect = Status;
        int num_turns = 0;
        if(Status=="Sleep") 
            num_turns = Utility.Get_rand(1, 5);
        p.status.Get_statusEffect(num_turns);
    }
    void flinch_enemy()
    {
        if (!current_turn.move_.Can_flinch) return;
        if (Utility.Get_rand(1, 11) < current_turn.move_.Status_chance)
            current_turn.victim_.pokemon.isFlinched=true;
    }
    void Set_buff_Debuff()
    {
        if (!current_turn.move_.is_Buff_Debuff) return;
        if (Utility.Get_rand(1, 101) > current_turn.move_.Debuff_chance)
        {
            Debug.Log("debuff failed");
            return;
        }
        string effect = current_turn.move_.Buff_Debuff;
        char result = effect[1];//buff or debuff
        int buff_amount = int.Parse(effect[2].ToString());
        char reciever = effect[0];//who the change is effecting
        string stat = effect.Substring(3, effect.Length - 3);
        Debug.Log(stat+" "+result +buff_amount);
        switch (stat.ToLower())
        {
            case"Defense":
                current_turn.victim_.pokemon.Defense = Get_buff_debuff(current_turn.victim_.pokemon.Defense,stat,buff_amount,reciever,result);
                    break;
            case"Attack":
                current_turn.victim_.pokemon.Attack = Get_buff_debuff(current_turn.victim_.pokemon.Attack,stat,buff_amount,reciever,result);
                break;
            case"Special Defense":
                current_turn.victim_.pokemon.SP_DEF = Get_buff_debuff(current_turn.victim_.pokemon.SP_DEF,stat,buff_amount,reciever,result);
                break;
            case"Special Attack":
                current_turn.victim_.pokemon.SP_ATK = Get_buff_debuff(current_turn.victim_.pokemon.SP_ATK,stat,buff_amount,reciever,result);
                break;
            case"Speed":
                current_turn.victim_.pokemon.speed = Get_buff_debuff(current_turn.victim_.pokemon.speed,stat,buff_amount,reciever,result);
                break;
            case"Accuracy":
                current_turn.victim_.pokemon.Accuracy = Get_buff_debuff(current_turn.victim_.pokemon.Accuracy,stat,buff_amount,reciever,result);
                break;
            case"Evasion":
                current_turn.victim_.pokemon.Evasion = Get_buff_debuff(current_turn.victim_.pokemon.Evasion,stat,buff_amount,reciever,result);
                break;
            case"Crit":
                current_turn.victim_.pokemon.crit_chance = Get_buff_debuff(current_turn.victim_.pokemon.crit_chance,stat,buff_amount,reciever,result);
                break;
        }
    }
    float Get_buff_debuff(float stat_val,string stat,int buff_amount,char reciever,char result)
    {
        Pokemon reciever_pkm;
        string result_txt;
        if (reciever == 'e')
            reciever_pkm = current_turn.victim_.pokemon;
        else
            reciever_pkm = current_turn.attacker_.pokemon;
        if (result == '+')
        {
            BattleOperations.ChangeBuffs(reciever_pkm, stat,buff_amount, true);
            result_txt = "Increased";
        }
        else
        {
            BattleOperations.ChangeBuffs(reciever_pkm, stat,buff_amount, false);
            result_txt = "Decreased";
        }
        Buff_Debuff buff = BattleOperations.GetBuff(reciever_pkm, stat);
        Debug.Log(reciever_pkm.Pokemon_name+"'s "+buff.Stat+" "+result_txt+"!");
        Dialogue_handler.instance.Battle_Info(reciever_pkm.Pokemon_name+"'s "+buff.Stat+" "+result_txt+"!");
        if (stat == "Accuracy" | stat == "Evasion")
            return math.trunc(stat_val * Accuracy_Evasion_Levels[buff.Stage+6]); 
        if(stat=="Crit")    
            return math.trunc(stat_val * Crit_Levels[buff.Stage]);
        return math.trunc(stat_val * Stat_Levels[buff.Stage+6]);
    }
    void absorb()
    {
        float damage = Calc_Damage();
        float enemy_previous_hp = current_turn.victim_.pokemon.HP;
        current_turn.victim_.pokemon.HP -= damage;
        float heal_amount = math.abs(current_turn.victim_.pokemon.HP - enemy_previous_hp);
        current_turn.attacker_.pokemon.HP += math.trunc(heal_amount/2f);
    }
    void doublekick()
    {
        //randopm amount of consecutive hits
    }
    void bulletseed()
    {
        //randopm amount of consecutive hits
    }
    void protect()
    {
        //success rate decreases
    }
    void magnitude()
    {
        //random damage pool
    }
}
