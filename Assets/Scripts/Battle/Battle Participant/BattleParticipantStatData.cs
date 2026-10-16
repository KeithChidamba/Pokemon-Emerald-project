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
        attack = participant.Pokemon.attack;
        spAtk = participant.Pokemon.specialAttack;
        defense = participant.Pokemon.defense;
        spDef = participant.Pokemon.specialDefense;
        speed = participant.Pokemon.speed;
        accuracy = participant.Pokemon.accuracy;
        evasion = participant.Pokemon.evasion;
        crit = participant.Pokemon.critChance;
    }
    public void LoadActualStats(bool fullReset=false)
    {
        participant.Pokemon.attack=attack;
        participant.Pokemon.specialAttack=spAtk;
        participant.Pokemon.defense=defense;
        participant.Pokemon.specialDefense=spDef;
        participant.Pokemon.speed=speed;
        if (fullReset)
        {
            participant.Pokemon.accuracy = 100;
            participant.Pokemon.evasion = 100;
            participant.Pokemon.critChance = 6.25f;
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
