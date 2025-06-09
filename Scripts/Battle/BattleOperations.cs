using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public static class BattleOperations
{
    private static float _effectiveness = 0;
    public static bool CanDisplayDialougue = true;
    public static bool CheckImmunity(Pokemon victim,Type enemyType)
    {
        foreach(var type in victim.types)
            if (PokemonOperations.ContainsType(type.immunities,enemyType))
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
            if (PokemonOperations.ContainsType(t.resistances, enemyType))
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
            if (victim.additionalTypeImmunity.typeName == enemyType.typeName)
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
        if (statusName == "Sleep" || statusName == "Freeze")
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
    private static bool HasBuffOrDebuff(Pokemon pokemon,string statName)
    {
        return pokemon.buffAndDebuffs.Any(b=>b.stat==statName);
    }
    public static void ChangeOrCreateBuffOrDebuff(BuffDebuffData data)
    {
        if (!HasBuffOrDebuff(data.Receiver.pokemon, data.StatName))
        {
            data.Receiver.pokemon.buffAndDebuffs.Add(CreateNewBuff(data.StatName));
        }
        var buff = SearchForBuffOrDebuff(data.Receiver.pokemon, data.StatName);//wont ever be null
        buff.stage = ValidateBuffLimit(data.Receiver.pokemon, buff, data.IsIncreasing, data.EffectAmount);
        CanDisplayDialougue = true;
        RemoveInvalidBuffsOrDebuffs(data.Receiver.pokemon);
    }

    private static int ValidateBuffLimit(Pokemon pkm,Buff_Debuff buff,bool increased,int changeValue)
    {
        var change = 0;
        var message="";
        var indexLimitHigh = (buff.stat == "Crit") ? 2 : 5;
        var indexLimitLow = (buff.stat == "Crit") ? 1 : -5;
        if (buff.stage > indexLimitHigh && increased)
        {
            buff.isAtLimit = true;
            if(CanDisplayDialougue)
                Dialogue_handler.Instance.DisplayBattleInfo(pkm.pokemonName+"'s "+buff.stat+" cant go any higher");
            return buff.stage;
        }
        if (buff.stage < indexLimitLow && !increased)
        {
            buff.isAtLimit = true;
            if(CanDisplayDialougue)
                Dialogue_handler.Instance.DisplayBattleInfo(pkm.pokemonName+"'s "+buff.stat+" cant go any lower");
            return buff.stage;;
        }
        if (increased)
        {
            change = buff.stage+changeValue;
            message = pkm.pokemonName+"'s "+buff.stat+" Increased!";
        }
        else
        {
            change = buff.stage-changeValue;
            message = pkm.pokemonName+"'s "+buff.stat+" Decreased!";
        }
        if(CanDisplayDialougue)
            Dialogue_handler.Instance.DisplayBattleInfo(message);
        if(change>indexLimitHigh)
            return indexLimitHigh + 1;
        if(change<indexLimitLow)
            return indexLimitLow - 1; 
        return change;
    }
    private static Buff_Debuff CreateNewBuff(string statName)
    {
        return new Buff_Debuff(statName,0,false);
    }
    public static Buff_Debuff SearchForBuffOrDebuff(Pokemon pokemon,string statName)
    {
        return pokemon.buffAndDebuffs.FirstOrDefault(b=>b.stat==statName);
    }
    private static void RemoveInvalidBuffsOrDebuffs(Pokemon pokemon)
    {
        pokemon.buffAndDebuffs.RemoveAll(b=>b.stage==0);
        Move_handler.Instance.processingOrder = false;
    }
}
