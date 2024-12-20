using UnityEngine;

public static class Utility
{
    private static float effectiveness = 0;
    public static int Get_rand(int min,int exclusive_lim)
    {
        return Random.Range(min, exclusive_lim);
    }
    public static bool isImmuneTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.Non_effect,enemy_type))
                return true;
        return false;
    } 
    static void isWeakTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.weaknesses, enemy_type))
            {
                //Debug.Log(victim.Pokemon_name + " is weak to "+enemy_type.Type_name);
                effectiveness *= 2f;
            }
    }
    static void isResistantTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.Resistances, enemy_type))
            {
                //Debug.Log(victim.Pokemon_name + " is resistant to "+enemy_type.Type_name);
                effectiveness /= 2f;
            }
    }

    public static bool is_Stab(Pokemon pkm,Type move)
    {
        foreach(Type t in pkm.types)
            if (t == move)
                return true;
        return false;
    }
    public static float TypeEffectiveness(Pokemon victim,Type enemy_type)
    {
        if (isImmuneTo(victim, enemy_type))
        {
              effectiveness = 0;
              //Debug.Log(victim.Pokemon_name + " is immune to "+enemy_type.Type_name);
        }
        else
        {
            effectiveness = 1;
            isWeakTo(victim, enemy_type);
            isResistantTo(victim, enemy_type);
        }
        return effectiveness;
    }

    public static bool Hasbuff_Debuff(Pokemon pkm,string stat_name)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stat == stat_name)
                return true;
        return false;
    }
    public static void ChangeBuffs(Pokemon pkm,string stat_name,bool increase)
    {
        if(Hasbuff_Debuff(pkm, stat_name))
        {
            foreach (Buff_Debuff b in pkm.Buff_Debuffs)
                if (b.Stat == stat_name)
                {
                    if (increase)
                        b.Stage+=CheckBuffLimit(b);
                    else
                        b.Stage-=CheckBuffLimit(b);
                }
        }else
            pkm.Buff_Debuffs.Add(NewBuff(stat_name, increase));
        CheckBuffs(pkm);
    }
    static int CheckBuffLimit(Buff_Debuff buff)
    {
        int change = 0;
        if (buff.Stat == "crit")
        {
            if (buff.Stage > -1 & buff.Stage < 3)
                change = 1;
        }
        else if (buff.Stage < 6 & buff.Stage > -6)
                change = 1;
        return change;
    }
    private static Buff_Debuff NewBuff(string stat_name,bool increase)
    {
        int Stage = 0;
        if (increase)
            Stage++;
        else
            Stage--;
        return new Buff_Debuff(stat_name, Stage);
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
