using Unity.Mathematics;
using UnityEngine;

public class Move_handler:MonoBehaviour
{
    public bool Doing_move = false;
    public static Move_handler instance;
    private Turn current_turn;
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
        Dialogue_handler.instance.Write_Info(turn.attacker_.Pokemon_name+" used "+turn.move_.Move_name+" on "+turn.victim_.Pokemon_name+"!","Battle info");
        if(!current_turn.move_.is_Buff_Debuff)
            Invoke(nameof(Deal_Damage),1.4f);//call this in move effect instead of here. this is just for testing
        else
        {
            //just do move effect
            //Dialogue_handler.instance.Write_Info("The move effect is happening", "Battle info"); 
            Invoke(nameof(Move_done),1f);
        }
        //call appropriate move for move effect
        //invoke move_name+effect methods
    }

    void Deal_Damage()
    {
        float damage_dealt=0;
        float level_factor = (float)((current_turn.attacker_.Current_level * 2) / 5) + 2;
        float Stab = 1f;
        int crit = 1;
        float Attack_type = 0;
        float atk_def_ratio;
        float random_factor = (float)Utility.Get_rand(85, 101) / 100;
        float type_effectiveness = Utility.TypeEffectiveness(current_turn.victim_, current_turn.move_.type);
        if (current_turn.move_.isSpecial)
        {
            Attack_type = current_turn.attacker_.SP_ATK;
            atk_def_ratio = Attack_type / current_turn.victim_.SP_DEF;
        }
        else
        {
            Attack_type = current_turn.attacker_.Attack;
            atk_def_ratio = Attack_type / current_turn.victim_.Defense;
        }
        if (Utility.is_Stab(current_turn.attacker_, current_turn.move_.type))
            Stab = 1.5f;
        if (Utility.Get_rand(1, (int)(100/current_turn.attacker_.crit_chance)+1)<2)
            crit = 2;
        float base_Damage = (level_factor * current_turn.move_.Move_damage *
                            (Attack_type/ current_turn.move_.Move_damage))/current_turn.attacker_.Current_level;
        float Modifier = crit*Stab*random_factor*type_effectiveness;
        damage_dealt = Modifier * base_Damage * atk_def_ratio;
        current_turn.victim_.HP -= math.trunc(damage_dealt);
        if (type_effectiveness > 1)
            Dialogue_handler.instance.Write_Info("The move is Super effective!", "Battle info");
        Invoke(nameof(Move_done),1f);
    }

    void Move_done()
    {
        Doing_move = false;
    }
}
