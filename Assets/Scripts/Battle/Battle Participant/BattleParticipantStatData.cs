using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
[Serializable]
public class BattleParticipantStatData
{
    public float attack;
    public float defense;
    public float spAtk;
    public float spDef;
    public float speed;
    public float accuracy;
    public float evasion;
    public float crit;

    public BattleParticipant participant;
    
    public BattleParticipantStatData(BattleParticipant parentParticipant)
    {
        participant = parentParticipant;
    }
    public void SaveActualStats()
    {
        attack = participant.pokemon.attack;
        spAtk = participant.pokemon.specialAttack;
        defense = participant.pokemon.defense;
        spDef = participant.pokemon.specialDefense;
        speed = participant.pokemon.speed;
        accuracy = participant.pokemon.accuracy;
        evasion = participant.pokemon.evasion;
        crit = participant.pokemon.critChance;
    }
    public void LoadActualStats(bool fullReset=false)
    {
        participant.pokemon.attack=attack;
        participant.pokemon.specialAttack=spAtk;
        participant.pokemon.defense=defense;
        participant.pokemon.specialDefense=spDef;
        participant.pokemon.speed=speed;
        if (fullReset)
        {
            participant.pokemon.accuracy = 100;
            participant.pokemon.evasion = 100;
            participant.pokemon.critChance = 6.25f;
        }
    }
    public void ResetBattleState(Pokemon pokemon,bool justLeveledUp = false)
    {
        pokemon.accuracy = 100;
        pokemon.evasion = 100;
        pokemon.critChance = 6.25f;
        pokemon.statModifiers.Clear();
        if (justLeveledUp) return;
        
        participant.canAttack = true;
        participant.canBeDamaged = true;
        
        participant.isFlinched = false;
        participant.canBeFlinched = true;
        
        participant.isConfused = false;
        
        participant.isInfatuated = false;
        participant.canBeInfatuated = true;
        
        var rawName = pokemon.pokemonDisplayName.Replace("Foe ", "");
        pokemon.pokemonDisplayName = rawName;
    }
}
