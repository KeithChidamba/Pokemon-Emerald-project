using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class BattleOperations : MonoBehaviour,IInjectable
{   
    public event Action OnBuffApplied;
        
    private BattleVisuals _battleVisualsHandler;
    private PokemonOperations _pokemonOperations;
    
    public void Inject(ServiceContainer container)
    {
        _battleVisualsHandler = container.Resolve<BattleVisuals>();
        _pokemonOperations = container.Resolve<PokemonOperations>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        
    }
    
    public bool HasImmunity(Pokemon victim,Type enemyType)
    {
        foreach(var type in victim.types)
            if (_pokemonOperations.ContainsType(type.immunities,enemyType))
                return true;
        return false;
    } 
    
    public bool IsStab(Pokemon pokemon,Type moveType)
    {
        foreach(Type t in pokemon.types)
            if (t == moveType)
                return true;
        return false;
    }
    public float CheckTypeEffectiveness(Battle_Participant victim,Type enemyType)
    {
        float effectiveness = 1;
        if (victim.additionalTypeImmunity!=null)
        {
            if (victim.additionalTypeImmunity.typeEnum == enemyType.typeEnum)
                effectiveness = 0;
        }
        else{
            if (HasImmunity(victim.pokemon, enemyType)) 
            {
                //if victim had their immunity altered by moves, like foresight
                effectiveness = victim.immunityNegations
                    .Any(negation => negation.ImmunityNegationTypes
                        .Any(type=>type == enemyType.typeEnum)) ? 1 : 0;
            }
            else
            {
                effectiveness = GetTypeEffectiveness(victim.pokemon, enemyType);
            }
        }
        return effectiveness;
    }
    public float GetTypeEffectiveness(Pokemon victim,Type enemyType)
    {
        float effectiveness = 1;
        //Weakness
        foreach(Type t in victim.types)
            if (_pokemonOperations.ContainsType(t.weaknesses, enemyType))
                effectiveness *= 2f;
        //Resistance
        foreach(Type t in victim.types)
            if (_pokemonOperations.ContainsType(t.resistances, enemyType))
                effectiveness /= 2f;
        
        return effectiveness;
    }
    public bool HardCountered(Pokemon victim,Pokemon enemy)
    {
        foreach (var type in victim.types)
        {
            return HasImmunity(enemy, type);
        }
        return false;
    }
    //Pokeballs
    public float GetCatchRateBonusFromStatus(StatusEffect statusName)
    {
        if (statusName == StatusEffect.None) return 1;
        if (statusName == StatusEffect.Sleep || statusName == StatusEffect.Freeze)
            return 2.5f;
        return 1.5f;
    }
    public bool IsImmediateCatch(float catchValue)
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
public string AttemptBuffOperation(BuffDebuffData data)
{
    var desiredBuff = SearchForBuffOrDebuff(data.Receiver.pokemon, data.Stat);
    if (desiredBuff == null)
    {
        desiredBuff = CreateNewBuff(data.Stat);
        data.Receiver.pokemon.buffAndDebuffs.Add(desiredBuff);
    }

    string message;
    bool increased = data.IsIncreasing;

    int upperLimit = desiredBuff.stat == Stat.Crit ? 2 : 5;
    int lowerLimit = desiredBuff.stat == Stat.Crit ? 1 : -5;

    int oldStage = desiredBuff.stage;
    int delta = increased ? data.EffectAmount : -data.EffectAmount;
    int newStage = math.clamp(oldStage + delta, lowerLimit, upperLimit);

    if (newStage == oldStage)
    {
        desiredBuff.isAtLimit = true;

        message = increased
            ? $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} can't go any higher!"
            : $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} can't go any lower!";

        _battleVisualsHandler.CancelBuffVisual();
    }
    else
    {
        desiredBuff.isAtLimit = false;
        desiredBuff.stage = newStage;

        int actualChange = math.abs(newStage - oldStage);

        if (increased)
        {
            message = actualChange switch
            {
                1 => $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} rose!",
                2 => $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} rose sharply!",
                _ => $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} rose drastically!"
            };
        }
        else
        {
            message = actualChange switch
            {
                1 => $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} fell!",
                2 => $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} harshly fell!",
                _ => $"{data.Receiver.pokemon.pokemonDisplayName}'s {desiredBuff.statName} severely fell!"
            };
        }
    }

    RemoveInvalidBuffsOrDebuffs(data.Receiver.pokemon);
    OnBuffApplied?.Invoke();

    return message;
}
    public string GetBuffResultMessage(bool isIncreasing,Pokemon pokemon,Stat[] buffs)
    {
        //shorten stat names to be more readable
        string buffNameString=""; 
        List<string> shortBuffNames = new();
        foreach (var buff in buffs)
        {
            shortBuffNames.Add(NameDB.GetShortStatName(buff));
        }
        for (int i = 0; i < shortBuffNames.Count; i++) 
        {
            if (i == shortBuffNames.Count - 1)
                buffNameString += shortBuffNames[i];
            else if(i == shortBuffNames.Count - 2)
                buffNameString += shortBuffNames[i] + " and ";
            else
                buffNameString += shortBuffNames[i] + ", ";
        }
        if(isIncreasing) return pokemon.pokemonDisplayName+"'s "+buffNameString+" rose";
        
        return pokemon.pokemonDisplayName+"'s "+buffNameString+" fell";
    }

    private Buff_Debuff CreateNewBuff( Stat statName)
    {
        return new Buff_Debuff(statName,0);
    }
    public Buff_Debuff SearchForBuffOrDebuff(Pokemon pokemon, Stat stat)
    {
        return pokemon.buffAndDebuffs.FirstOrDefault(b=>b.stat==stat);
    }
    public void RemoveInvalidBuffsOrDebuffs(Pokemon pokemon)
    {
        pokemon.buffAndDebuffs.RemoveAll(b=>b.stage==0);
    }

    public void ModifyBuff(Buff_Debuff buff, int limitHigh, int change)
    {
        buff.stage = math.clamp(buff.stage + change, buff.stage, limitHigh);
    }
}
