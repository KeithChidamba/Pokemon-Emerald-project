using UnityEngine;

public static class BattleOperations
{
    private static float effectiveness = 0;
    public static bool isImmuneTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (enemy_type.type_check(enemy_type.Non_effect, t))
            {
                //Debug.Log(t +" is immune to "+ enemy_type);
                return true;
            }
        return false;
    } 
    static void isWeakTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.weaknesses, enemy_type))
            {
                //Debug.Log(enemy_type +" is effective against "+ t);
                effectiveness *= 2f;
            }
    }
    static void isResistantTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.Resistances, enemy_type))
            {
                //Debug.Log(enemy_type +" is not effective against "+ t);
                effectiveness /= 2f;
            }
    }
    public static bool is_Stab(Pokemon pkm,Type move_type)
    {
        foreach(Type t in pkm.types)
            if (t == move_type)
                return true;
        return false;
    }
    public static float TypeEffectiveness(Pokemon victim,Type enemy_type)
    {
        if (isImmuneTo(victim, enemy_type))
              effectiveness = 0;
        else
        {
            effectiveness = 1;
            isWeakTo(victim, enemy_type);
            isResistantTo(victim, enemy_type);
        }
        return effectiveness;
    }
//Buffs
    public static bool Hasbuff_Debuff(Pokemon pkm,string stat_name)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stat == stat_name)
                return true;
        return false;
    }
    public static void ChangeBuffs(Pokemon pkm,string stat_name,int buff_amount,bool increase)
    {
        if(Hasbuff_Debuff(pkm, stat_name))
        {
            foreach (Buff_Debuff b in pkm.Buff_Debuffs)
                if (b.Stat == stat_name)
                        b.Stage=CheckBuffLimit(pkm,b,increase,buff_amount);
        }
        else
        {
            pkm.Buff_Debuffs.Add(NewBuff(stat_name));
            ChangeBuffs(pkm,stat_name,buff_amount,increase);
            return;
        }
        CheckBuffs(pkm);
    }
    static int CheckBuffLimit(Pokemon pkm,Buff_Debuff buff,bool increased,int buff_amount)
    {
        int change = 0;
        int limit_high;
        int limit_low;
        if (buff.Stat == "Crit")
        {
            limit_high = 2;
            limit_low = 1;
        }
        else
        {
            limit_high = 5;
            limit_low = -5;
        }
        if (buff.Stage > limit_high && increased)
        {
            Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" cant go any higher");
            return 0;
        }
        if (buff.Stage < limit_low && !increased)
        {
            Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" cant go any lower");
            return 0;
        }
        if (increased)
        {
            change = buff.Stage+buff_amount;
            Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" Increased!");
        }
        else
        {
            change = buff.Stage-buff_amount;
            Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" Decreased!");
        }
        Debug.Log("changed: "+change+" limit high "+ (limit_high + 1) +" limit low "+(limit_low - 1));
        if(change>limit_high)
            return limit_high + 1;
        if(change<limit_low)
            return limit_low - 1; 
        return change;
    }
    private static Buff_Debuff NewBuff(string stat_name)
    {
        Buff_Debuff buff = ScriptableObject.CreateInstance<Buff_Debuff>();
        buff.Stat = stat_name;
        buff.Stage = 0;
        return buff;
    }
    public static Buff_Debuff GetBuff(Pokemon pkm,string stat_name)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stat == stat_name)
                return b;
        return null;
    }
    private static void CheckBuffs(Pokemon pkm)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stage==0)
                pkm.Buff_Debuffs.Remove(b);
    }
}
