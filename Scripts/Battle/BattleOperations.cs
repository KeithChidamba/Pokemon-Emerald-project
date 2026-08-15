using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class StatChangeOperationData
{
    public StatChangeTransitData statChangeData;
    public StatChangeData finalStatData;

    public StatChangeOperationData(StatChangeTransitData statChangeData, StatChangeData finalStatData)
    {
        this.statChangeData = statChangeData;
        this.finalStatData = finalStatData;
    }
}

public class BattleOperations : MonoBehaviour,IInjectable
{   
    public event Action<StatChangeOperationData> OnStatChangeApplied;
        
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
    public float CheckTypeEffectiveness(BattleParticipant victim,Type moveType)
    {
        float effectiveness = 1;
        if (victim.additionalTypeImmunity!=null)
        {
            if (victim.additionalTypeImmunity.typeEnum == moveType.typeEnum)
                effectiveness = 0;
        }
        else{
            if (HasImmunity(victim.pokemon, moveType)) 
            {
                //if victim had their immunity altered by moves, like foresight
                effectiveness = victim.immunityNegations
                    .Any(negation => negation.ImmunityNegationTypes
                        .Any(type=>type == moveType.typeEnum)) ? 1 : 0;
            }
            else
            {
                effectiveness = GetTypeEffectiveness(victim.pokemon, moveType);
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
    
public string AttemptStatChangeOperation(StatChangeTransitData data)
{
    var desiredModifier = SearchForStatModifier(data.receiver.pokemon, data.stat);
    if (desiredModifier == null)
    {
        desiredModifier = CreateNewStatModifier(data.stat); 
        data.receiver.pokemon.statModifiers.Add(desiredModifier);
    }

    string message;
    bool increased = data.isIncreasing;

    int upperLimit = desiredModifier.stat == Stat.Crit ? 2 : 5;
    int lowerLimit = desiredModifier.stat == Stat.Crit ? 1 : -5;
    
    int oldStage = desiredModifier.stage;
   
    int delta = increased ? data.effectAmount : -data.effectAmount;
    
    int newStage = math.clamp(oldStage + delta, lowerLimit, upperLimit);
    
    if (newStage == oldStage)
    {
        desiredModifier.isAtLimit = true;

        message = increased
            ? $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} can't go any higher!"
            : $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} can't go any lower!";

        _battleVisualsHandler.CancelStatChangeVisual();
    }
    else
    {
        desiredModifier.isAtLimit = false;
        desiredModifier.stage = newStage;

        int actualChange = math.abs(newStage - oldStage);

        if (increased)
        {
            message = actualChange switch
            {
                1 => $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} rose!",
                2 => $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} rose sharply!",
                _ => $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} rose drastically!"
            };
        }
        else
        {
            message = actualChange switch
            {
                1 => $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} fell!",
                2 => $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} harshly fell!",
                _ => $"{data.receiver.pokemon.pokemonDisplayName}'s {desiredModifier.statName} severely fell!"
            };
        }
    }
    
    OnStatChangeApplied?.Invoke(new(data,desiredModifier));

    return message;
}
    public string GetStatModResultMessage(bool isIncreasing,Pokemon pokemon,Stat[] stats)
    {
        //shorten stat names to be more readable
        string statNameString = ""; 
        List<string> shortStatNames = new();
        foreach (var stat in stats)
        {
            shortStatNames.Add(NameDB.GetShortStatName(stat));
        }
        for (int i = 0; i < shortStatNames.Count; i++) 
        {
            if (i == shortStatNames.Count - 1) 
                statNameString += shortStatNames[i];
            else if(i == shortStatNames.Count - 2)
                statNameString += shortStatNames[i] + " and ";
            else
                statNameString += shortStatNames[i] + ", ";
        }
        if(isIncreasing) return pokemon.pokemonDisplayName+"'s "+statNameString+" rose";
        
        return pokemon.pokemonDisplayName+"'s "+statNameString+" fell";
    }

    private StatChangeData CreateNewStatModifier( Stat statName)
    {
        return new StatChangeData(statName,0);
    }
    public StatChangeData SearchForStatModifier(Pokemon pokemon, Stat stat)
    {
        return pokemon.statModifiers.FirstOrDefault(b=>b.stat==stat);
    }
}
