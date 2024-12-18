using System;
using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour
{
    public bool Doing_move = false;
    public static Move_handler instance;
    private Turn current_turn;
    private readonly float[] Stat_Levels = new float[13]{0.25f,0.29f,0.33f,0.4f,0.5f,0.67f,1f,1.5f,2f,2.5f,3f,3.5f,4f};
    private readonly float[] Accuracy_Evasion_Levels = new float[13]{0.33f,0.375f,0.43f,0.5f,0.6f,0.75f,1f,1.33f,1.67f,2f,2.33f,2.67f,3f};
    private readonly float[] Crit_Levels = new float[4]{6.25f,12.5f,25f,50f};
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Do_move(Turn turn)
    {
        current_turn=turn;
        Dialogue_handler.instance.Write_Info(turn.attacker_.pokemon.Pokemon_name+" used "+turn.move_.Move_name+" on "+turn.victim_.pokemon.Pokemon_name+"!","Battle info");
        Invoke(nameof(Move_effect),1f);//remove after adding animations
        //Invoke(nameof(PlayAnimation),1f);
    }
    void PlayAnimation()
    {
        //call anim script and run move_name as anim
        //Move_effect in anim event
    }
    void Move_effect()//anim event
    {
        Invoke(nameof(Deal_Damage),1f);//remove this, just testing
        if (current_turn.move_.Has_status)
            Get_status();
        if (current_turn.move_.is_Buff_Debuff)
            Set_buff_Debuff(current_turn.move_.Buff_Debuff);
        if(current_turn.move_.Can_flinch)
            flinch_enemy();
        try
        {
            Invoke(current_turn.move_.Move_name.ToLower(), 0f);
        } //remove spaces
        catch(Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    float Calc_Damage()
    {
        float damage_dealt=0;
        float level_factor = (float)((current_turn.attacker_.pokemon.Current_level * 2) / 5) + 2;
        float Stab = 1f;
        int crit = 1;
        float Attack_type = 0;
        float atk_def_ratio;
        float random_factor = (float)Utility.Get_rand(85, 101) / 100;
        float type_effectiveness = Utility.TypeEffectiveness(current_turn.victim_.pokemon, current_turn.move_.type);
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
        if (Utility.is_Stab(current_turn.attacker_.pokemon, current_turn.move_.type))
            Stab = 1.5f;
        if (Utility.Get_rand(1, (int)(100/current_turn.attacker_.pokemon.crit_chance)+1)<2)
            crit = 2;
        float base_Damage = (level_factor * current_turn.move_.Move_damage *
                             (Attack_type/ current_turn.move_.Move_damage))/current_turn.attacker_.pokemon.Current_level;
        float Modifier = crit*Stab*random_factor*type_effectiveness;
        damage_dealt = math.trunc(Modifier * base_Damage * atk_def_ratio);
        if (type_effectiveness > 1)
            Dialogue_handler.instance.Write_Info("The move is Super effective!", "Battle info");
        return damage_dealt;
    }
    void Deal_Damage()//anim event
    {
        if (current_turn.victim_.pokemon.CanBeDamaged)
            current_turn.victim_.pokemon.HP -= Calc_Damage();
        else
            Dialogue_handler.instance.Write_Info(current_turn.victim_.pokemon.Pokemon_name+" protected itself", "Battle info");
        Invoke(nameof(Move_done),1f);
    }
    void Move_done()
    {
        Doing_move = false;
    }
    void Get_status()
    {
        if (Utility.Get_rand(1, 101) < current_turn.move_.Status_chance)
        {
            current_turn.victim_.pokemon.Status_effect = current_turn.move_.Status_effect;
            int num_turns = 0;
            if(current_turn.move_.Status_effect=="Sleep") 
                num_turns = Utility.Get_rand(1, 5);
            current_turn.victim_.Get_statusEffect(num_turns);
        }
    }
    void flinch_enemy()
    {
        if (Utility.Get_rand(1, 11) < current_turn.move_.Status_chance)
            current_turn.victim_.pokemon.isFlinched=true;
    }
    void Set_buff_Debuff(string effect)
    {
        char result = effect[1];
        char reciever = effect[0];
        string stat = effect.Substring(2, effect.Length - 3);
        switch (stat.ToLower())
        {
            case"def":
                current_turn.victim_.pokemon.Defense = Get_buff_debuff(current_turn.victim_.pokemon.Defense,stat,reciever,result);
                    break;
            case"atk":
                current_turn.victim_.pokemon.Attack = Get_buff_debuff(current_turn.victim_.pokemon.Attack,stat,reciever,result);
                break;
            case"sp_def":
                current_turn.victim_.pokemon.SP_DEF = Get_buff_debuff(current_turn.victim_.pokemon.SP_DEF,stat,reciever,result);
                break;
            case"sp_atk":
                current_turn.victim_.pokemon.SP_ATK = Get_buff_debuff(current_turn.victim_.pokemon.SP_ATK,stat,reciever,result);
                break;
            case"spd":
                current_turn.victim_.pokemon.speed = Get_buff_debuff(current_turn.victim_.pokemon.speed,stat,reciever,result);
                break;
            case"acc":
                current_turn.victim_.pokemon.Accuracy = Get_buff_debuff(current_turn.victim_.pokemon.Accuracy,stat,reciever,result);
                break;
            case"eva":
                current_turn.victim_.pokemon.Evasion = Get_buff_debuff(current_turn.victim_.pokemon.Evasion,stat,reciever,result);
                break;
            case"crit":
                current_turn.victim_.pokemon.crit_chance = Get_buff_debuff(current_turn.victim_.pokemon.crit_chance,stat,reciever,result);
                break;
        }
    }
    float Get_buff_debuff(float stat_val,string stat,char reciever,char result)
    {
        int buff_Debuff=0;
        Pokemon reciever_pkm;
        if (reciever == 'e')
            reciever_pkm = current_turn.victim_.pokemon;
        else
            reciever_pkm = current_turn.attacker_.pokemon;
        int current_buff = int.Parse(reciever_pkm.Buff_Debuff.Substring(current_turn.victim_.pokemon.Buff_Debuff.Length-2));
        if (stat == "crit")
        {
            if (current_buff < 3)
                if(result=='+')
                    buff_Debuff += 1;
                else 
                    if (current_buff > 1)
                        buff_Debuff = current_buff-1;
        }
        else if (current_buff < 6 & current_buff > -6)
        {
            if(result=='+')
                buff_Debuff = current_buff+1;
            else
                buff_Debuff = current_buff-1;
        }
        if(buff_Debuff!=0)
            reciever_pkm.Buff_Debuff = stat + result + buff_Debuff;
        else
            reciever_pkm.Buff_Debuff = "None";    
        if (stat == "acc" | stat == "eva")
            return stat_val * Accuracy_Evasion_Levels[buff_Debuff+6]; 
        if(stat=="crit")    
            return stat_val * Crit_Levels[buff_Debuff];
        return stat_val * Stat_Levels[buff_Debuff+6];
    }
    void absorb()
    {
        float damage = Calc_Damage();
        current_turn.victim_.pokemon.HP -= damage;
        current_turn.attacker_.pokemon.HP += math.trunc(damage/2f);
        Invoke(nameof(Move_done),1f);
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
