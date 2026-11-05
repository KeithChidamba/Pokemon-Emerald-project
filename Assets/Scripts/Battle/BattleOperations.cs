using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

[Serializable]
public class StatChangeOperationData
{
    public StatChangeTransitData statChangeData;
    public StatChangeData finalStatData;
    public string resultMessage;
    
    public StatChangeOperationData(StatChangeTransitData statChangeData, StatChangeData finalStatData
    , string message)
    {
        this.statChangeData = statChangeData;
        this.finalStatData = finalStatData;
        resultMessage = message;
    }
}

public class BattleOperations : MonoBehaviour,IInjectable
{   
    public event Action<StatChangeOperationData> OnStatChangeApplied;

    public void Inject(ServiceContainer container)
    {
        gameObject.SetActive(true);
    }
    public void OnInject() { }
    
    public bool IsStab(Pokemon pokemon,PokemonTypeData moveType)
    {
        return pokemon.types.Any(t=>t.typeEnum == moveType.typeEnum);
    }
    
    public float CheckTypeEffectiveness(BattleParticipant victim, PokemonTypeData moveType)
    {
        // Extra immunity granted by ability/move (Levitate, etc.)
        if (victim.additionalTypeImmunity is not null &&
            victim.additionalTypeImmunity.typeEnum == moveType.typeEnum)
            return 0;

        // Natural immunities, unless negated (Foresight, Odor Sleuth, etc.)
        if (HasImmunity())
        {
            bool negated = victim.immunityNegations
                .Any(n => n.ImmunityNegationTypes.Any(t => t == moveType.typeEnum));

            if (!negated) return 0;
            // negated: fall through and calculate normally
        }

        return GetTypeEffectiveness(victim.Pokemon, moveType);
        bool HasImmunity()
        {
            foreach(var type in victim.Pokemon.types)
                if (PokemonOperations.ContainsType(type.immunities,moveType))
                    return true;
            return false;
        } 
    }
    
    private float GetTypeEffectiveness(Pokemon victim,PokemonTypeData enemyType)
    {
        float effectiveness = 1;
        //Weakness
        foreach(PokemonTypeData t in victim.types)
            if (PokemonOperations.ContainsType(t.weaknesses, enemyType))
                effectiveness *= 2f;
        //Resistance
        foreach(PokemonTypeData t in victim.types)
            if (PokemonOperations.ContainsType(t.resistances, enemyType))
                effectiveness /= 2f;
        
        return effectiveness;
    }
    // All the victim's types must be blocked, and negations are respected
    public bool HardCountered(BattleParticipant victim, BattleParticipant enemy)
    {
        var types = victim.Pokemon.types;
        return types.Any() && types.All(t => CheckTypeEffectiveness(enemy, t) == 0);
    }

    /// <summary>
    /// True if the enemy is immune to every one of the victim's types (victim has no STAB that can hit it)
    /// </summary>
    public bool HardCountered(Pokemon victim, Pokemon enemy)
    {
        return victim.types.Any() && victim.types.All(victimType =>
            enemy.types.Any(enemyType => PokemonOperations.ContainsType(enemyType.immunities, victimType)));
    }

    // For scoring party members as switch-ins
    public float GetPartyTypeEffectiveness(Pokemon victim, PokemonTypeData enemyType)
    {
        if (victim.types.Any(t => PokemonOperations.ContainsType(t.immunities, enemyType))) return 0;
        return GetTypeEffectiveness(victim, enemyType);
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
    
    public StatChangeOperationData AttemptStatChangeOperation(StatChangeTransitData data)
    {
        var desiredModifier = data.receiver.Pokemon.statModifiers.FirstOrDefault(b=>b.stat==data.stat);
        if (desiredModifier is null)
        {
            desiredModifier = CreateNewStatModifier(data.stat); 
            data.receiver.Pokemon.statModifiers.Add(desiredModifier);
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
                ? $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} can't go any higher!"
                : $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} can't go any lower!";
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
                    1 => $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} rose!",
                    2 => $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} rose sharply!",
                    _ => $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} rose drastically!"
                };
            }
            else
            {
                message = actualChange switch
                {
                    1 => $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} fell!",
                    2 => $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} harshly fell!",
                    _ => $"{data.receiver.Pokemon.pokemonDisplayName}'s {desiredModifier.statName} severely fell!"
                };
            }
        }
        var result = new StatChangeOperationData(data, desiredModifier, message);
        OnStatChangeApplied?.Invoke(result);
        return result;
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
}
