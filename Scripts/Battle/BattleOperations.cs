using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class BattleOperations
{
    private static float _effectiveness = 0;
    public static bool CanDisplayChange = true;
    public static event Action OnBuffApplied;

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
    public static bool IsStab(Pokemon pokemon,Type moveType)
    {
        foreach(Type t in pokemon.types)
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
            {
                //if victim had their immunity altered by moves, like foresight
                _effectiveness = victim.immunityNegations
                    .Any(negation => negation.ImmunityNegationTypes
                        .Any(type=>type.ToString() == enemyType.typeName)) ? 1 : 0;
            }
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
    public static float GetCatchRateBonusFromStatus(PokemonOperations.StatusEffect statusName)
    {
        if (statusName == PokemonOperations.StatusEffect.None) return 1;
        if (statusName == PokemonOperations.StatusEffect.Sleep || statusName == PokemonOperations.StatusEffect.Freeze)
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
    
//Buffs
    private static bool HasBuffOrDebuff(Pokemon pokemon, PokemonOperations.Stat stat)
    {
        return pokemon.buffAndDebuffs.Any(b=>b.stat==stat);
    }
    public static void ChangeOrCreateBuffOrDebuff(BuffDebuffData data)
    {
        if (!HasBuffOrDebuff(data.Receiver.pokemon, data.Stat))
        {
            data.Receiver.pokemon.buffAndDebuffs.Add(CreateNewBuff(data.Stat));
        }
        var buff = SearchForBuffOrDebuff(data.Receiver.pokemon, data.Stat);//wont ever be null
        buff.stage = ValidateBuffLimit(data.Receiver, buff, data.IsIncreasing, data.EffectAmount);
        RemoveInvalidBuffsOrDebuffs(data.Receiver.pokemon);
        OnBuffApplied?.Invoke();
    }
    public static string GetBuffResultMessage(bool isIncreasing,Pokemon pokemon,PokemonOperations.Stat[] buffs)
    {
        string buffNames="";
        for (int i = 0; i < buffs.Length; i++)
        {
            if (i == buffs.Length - 1)
                buffNames += buffs[i];
            else if(i == buffs.Length - 2)
                buffNames += buffs[i] + " and ";
            else
                buffNames += buffs[i] + ", ";
        }
        if(isIncreasing) return pokemon.pokemonName+"'s "+buffNames+" rose";
        
        return pokemon.pokemonName+"'s "+buffNames+" fell";
    }
    private static int ValidateBuffLimit(Battle_Participant participant,Buff_Debuff buff,bool increased,int changeValue)
    {
        var change = 0;
        var message="";
        var indexLimitHigh = (buff.stat == PokemonOperations.Stat.Crit) ? 2 : 5;
        var indexLimitLow = (buff.stat ==  PokemonOperations.Stat.Crit) ? 1 : -5;
        if (buff.stage > indexLimitHigh && increased)
        {
            buff.isAtLimit = true;
            if(CanDisplayChange)
                Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemon.pokemonName+"'s "+buff.statName+" cant go any higher");
            BattleVisuals.Instance.CancelBuffVisual();
            return buff.stage;
        }
        if (buff.stage < indexLimitLow && !increased)
        {
            buff.isAtLimit = true;
            if(CanDisplayChange)
                Dialogue_handler.Instance.DisplayBattleInfo(participant.pokemon.pokemonName+"'s "+buff.statName+" cant go any lower");
            BattleVisuals.Instance.CancelBuffVisual();
            return buff.stage;
        }
        if (increased)
        {
            change = buff.stage+changeValue;
            message = participant.pokemon.pokemonName+"'s "+buff.statName+" rose!";
        }
        else
        {
            change = buff.stage-changeValue;
            message = participant.pokemon.pokemonName+"'s "+buff.statName+" fell!";
        }

        if (CanDisplayChange)
        {
            BattleVisuals.Instance.SelectStatChangeVisuals(buff.stat,participant,message);
        }
        if(change>indexLimitHigh)
            return indexLimitHigh + 1;
        if(change<indexLimitLow)
            return indexLimitLow - 1; 
        return change;
    }
    private static Buff_Debuff CreateNewBuff( PokemonOperations.Stat statName)
    {
        return new Buff_Debuff(statName,0,false);
    }
    public static Buff_Debuff SearchForBuffOrDebuff(Pokemon pokemon, PokemonOperations.Stat stat)
    {
        return pokemon.buffAndDebuffs.FirstOrDefault(b=>b.stat==stat);
    }
    public static void RemoveInvalidBuffsOrDebuffs(Pokemon pokemon)
    {
        pokemon.buffAndDebuffs.RemoveAll(b=>b.stage==0);
    }

    public static void ModifyBuff(Buff_Debuff buff, int limitHigh, int change)
    {
        buff.stage = math.clamp(buff.stage + change, buff.stage, limitHigh);
    }
}
