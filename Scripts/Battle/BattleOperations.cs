using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;

public static class BattleOperations
{
    private static float _effectiveness = 0;
    public static bool CanDisplayDialougue = true;
    public static bool CheckImmunity(Pokemon victim,Type enemyType)
    {
        foreach(Type t in victim.types)
            if (PokemonOperations.ContainsType(enemyType.Non_effect, t))
                return true;
        return false;
    } 
    private static void IsWeakTo(Pokemon victim,Type enemyType)
    {
        foreach(Type t in victim.types)
            if (PokemonOperations.ContainsType(t.weaknesses, enemyType))
                _effectiveness *= 2f;
    }
    private static void IsResistantTo(Pokemon victim,Type enemyType)
    {
        foreach(Type t in victim.types)
            if (PokemonOperations.ContainsType(t.Resistances, enemyType))
                _effectiveness /= 2f;
    }
    public static bool is_Stab(Pokemon pkm,Type moveType)
    {
        foreach(Type t in pkm.types)
            if (t == moveType)
                return true;
        return false;
    }
    public static float GetTypeEffectiveness(Battle_Participant victim,Type enemyType)
    {
        if (victim.additionalTypeImmunity!=null)
        {
            if (victim.additionalTypeImmunity == enemyType)
                _effectiveness = 0;
        }
        else{
            if (CheckImmunity(victim.pokemon, enemyType)) 
                _effectiveness = 0;
            else
            {
                _effectiveness = 1;
                IsWeakTo(victim.pokemon, enemyType);
                IsResistantTo(victim.pokemon, enemyType);
            }
        }
        return _effectiveness;
    }
    //Pokeballs
    public static float GetCatchRateBonusFromStatus(string statusName)
    {
        if (statusName == "None") return 1;
        if (statusName == "Sleep" | statusName == "Freeze")
            return 2.5f;
        return 1.5f;
    }
    public static bool IsImmediateCatch(float catchValue)
    {
        for (int i = 0; i < 4; i++)
        {
            var rand = Utility.RandomRange(0, 256);
            if (rand > catchValue)
                return false;
        }
        return true;
    }

    public static bool PassedPokeballShakeTest(float catchvalue)
    {
        float shakeProbability = 65536 / math.sqrt( math.sqrt(16711680/catchvalue)  );
        for (int i = 0; i < 3; i++)
        {
            int rand = Utility.Random16Bit();
            if (rand < (shakeProbability * (i+1)) )
                return true;
        }
        return false;
    }
//Buffs
    private static bool HasBuffOrDebuff(Pokemon pkm,string stat_name)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stat == stat_name)
                return true;
        return false;
    }
    public static void ChangeOrCreateBuffOrDebuff(BuffDebuffData data)
    {
        if (!HasBuffOrDebuff(data.Reciever, data.StatName))
        {
            data.Reciever.Buff_Debuffs.Add(CreateNewBuff(data.StatName));
        }
        foreach (Buff_Debuff buff in data.Reciever.Buff_Debuffs)
        {
            if (buff.Stat == data.StatName)
            {
                buff.Stage = ValidateBuffLimit(data.Reciever, buff, data.IsIncreasing, data.EffectAmount);
            }
        }
        CanDisplayDialougue = true;
        RemoveInvalidBuffsOrDebuffs(data.Reciever);
    }

    static int ValidateBuffLimit(Pokemon pkm,Buff_Debuff buff,bool increased,int changeValue)
    {
        var change = 0;
        var message="";
        var indexLimitHigh = (buff.Stat == "Crit") ? 2 : 5;
        var indexLimitLow = (buff.Stat == "Crit") ? 1 : -5;
        if (buff.Stage > indexLimitHigh && increased)
        {
            if(CanDisplayDialougue)
                Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" cant go any higher");
            return 0;
        }
        if (buff.Stage < indexLimitLow && !increased)
        {
            if(CanDisplayDialougue)
                Dialogue_handler.instance.Battle_Info(pkm.Pokemon_name+"'s "+buff.Stat+" cant go any lower");
            return 0;
        }
        if (increased)
        {
            change = buff.Stage+changeValue;
            message = pkm.Pokemon_name+"'s "+buff.Stat+" Increased!";
        }
        else
        {
            change = buff.Stage-changeValue;
            message = pkm.Pokemon_name+"'s "+buff.Stat+" Decreased!";
        }
        if(CanDisplayDialougue)
            Dialogue_handler.instance.Battle_Info(message);
        if(change>indexLimitHigh)
            return indexLimitHigh + 1;
        if(change<indexLimitLow)
            return indexLimitLow - 1; 
        return change;
    }
    private static Buff_Debuff CreateNewBuff(string statName)
    {
        Buff_Debuff buff = ScriptableObject.CreateInstance<Buff_Debuff>();
        buff.Stat = statName;
        buff.Stage = 0;
        return buff;
    }
    public static Buff_Debuff SearchForBuffOrDebuff(Pokemon pkm,string statName)
    {
        foreach (Buff_Debuff b in pkm.Buff_Debuffs)
            if (b.Stat == statName)
                return b;
        return null;
    }
    private static void RemoveInvalidBuffsOrDebuffs(Pokemon pkm)
    {
        foreach (Buff_Debuff b in new List<Buff_Debuff>(pkm.Buff_Debuffs))
            if (b.Stage==0)
                pkm.Buff_Debuffs.Remove(b);
        Move_handler.Instance.processingOrder = false;
    }
}
