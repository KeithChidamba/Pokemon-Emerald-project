using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;

public static class BattleOperations
{
    private static float effectiveness = 0;
    public static bool CanDisplayDialougue = true;
    public static bool isImmuneTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (enemy_type.type_check(enemy_type.Non_effect, t))
                return true;
        return false;
    } 
    static void isWeakTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.weaknesses, enemy_type))
                effectiveness *= 2f;
    }
    static void isResistantTo(Pokemon victim,Type enemy_type)
    {
        foreach(Type t in victim.types)
            if (t.type_check(t.Resistances, enemy_type))
                effectiveness /= 2f;
    }
    public static bool is_Stab(Pokemon pkm,Type move_type)
    {
        foreach(Type t in pkm.types)
            if (t == move_type)
                return true;
        return false;
    }
    public static float TypeEffectiveness(Battle_Participant victim,Type enemy_type)
    {
        if (victim.AddtionalTypeImmunity!=null)
        {
            if (victim.AddtionalTypeImmunity == enemy_type)
                effectiveness = 0;
        }
        else{
            if (isImmuneTo(victim.pokemon, enemy_type)) 
                effectiveness = 0;
            else
            {
                effectiveness = 1;
                isWeakTo(victim.pokemon, enemy_type);
                isResistantTo(victim.pokemon, enemy_type);
            }
        }
        return effectiveness;
    }
    //Pokeballs
    public static float GetStatusBonus(string statusName)
    {
        if (statusName == "None") return 1;
        if (statusName == "Sleep" | statusName == "Freeze")
            return 2.5f;
        return 1.5f;
    }
    public static bool IsImmediateCatch(float catchvalue)
    {
        for (int i = 0; i < 4; i++)
        {
            int rand = Utility.RandomRange(0, 256);
            if (rand > catchvalue)
                return false;
        }
        return true;
    }

    public static bool ShakeCheck(float catchvalue)
    {
        float ShakeProbability = 65536 / math.sqrt( math.sqrt(16711680/catchvalue)  );
        for (int i = 0; i < 3; i++)
        {
            int rand = Utility.Random16Bit();
            if (rand < (ShakeProbability * (i+1)) )
                return true;
        }
        return false;
    }
//Buffs
    public static bool Hasbuff_Debuff(Pokemon pkm,string stat_name)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stat == stat_name)
                return true;
        return false;
    }
    public static void ChangeBuffs(BuffDebuffData data)
    {
        if (!Hasbuff_Debuff(data.Reciever, data.StatName))
        {
            data.Reciever.Buff_Debuffs.Add(NewBuff(data.StatName));
        }
        foreach (Buff_Debuff buff in data.Reciever.Buff_Debuffs)
        {
            if (buff.Stat == data.StatName)
            {
                buff.Stage = CheckBuffLimit(data.Reciever, buff, data.isIncreasing, data.EffectAmount);
            }
        }
        CanDisplayDialougue = true;
        CheckBuffs(data.Reciever);
    }

    static int CheckBuffLimit(Pokemon pkm,Buff_Debuff buff,bool increased,int buff_amount)
    {
        int change = 0;
        string message="";
        int limit_high = (buff.Stat == "Crit") ? 2 : 5;
        int limit_low = (buff.Stat == "Crit") ? 1 : -5;
        if (buff.Stage > limit_high && increased)
        {
            if(CanDisplayDialougue)
                Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" cant go any higher");
            return 0;
        }
        if (buff.Stage < limit_low && !increased)
        {
            if(CanDisplayDialougue)
                Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" cant go any lower");
            return 0;
        }
        if (increased)
        {
            change = buff.Stage+buff_amount;
            message = pkm.Pokemon_name+"'s "+buff.Stat+" Increased!";
        }
        else
        {
            change = buff.Stage-buff_amount;
            message = pkm.Pokemon_name+"'s "+buff.Stat+" Decreased!";
        }
        if(CanDisplayDialougue)
            Dialogue_handler.instance.Battle_Info(message);
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
        foreach (Buff_Debuff b in new List<Buff_Debuff>(pkm.Buff_Debuffs))
            if (b.Stage==0)
                pkm.Buff_Debuffs.Remove(b);
        Move_handler.instance.ProcessingOrder = false;
    }
}
