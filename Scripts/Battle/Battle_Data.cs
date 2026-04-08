using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
[Serializable]
public class Battle_Data: BattleParticipantModule
{
    public float attack;
    public float defense;
    public float spAtk;
    public float spDef;
    public float speed;
    //keep this here for use in buffs and debuffs
    public float accuracy;
    public float evasion;
    public float crit;
    
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
    public void LoadActualStats()
    {
        participant.pokemon.attack=attack;
        participant.pokemon.specialAttack=spAtk;
        participant.pokemon.defense=defense;
        participant.pokemon.specialDefense=spDef;
        participant.pokemon.speed=speed;
    }
    public void ResetBattleState(Pokemon pokemon,bool justLeveledUp = false)
    {
        pokemon.accuracy = 100;
        pokemon.evasion = 100;
        pokemon.critChance = 6.25f;
        pokemon.buffAndDebuffs.Clear();
        if (justLeveledUp) return;
        participant.canAttack = true;
        participant.canBeDamaged = true;
        participant.isFlinched = false;
        participant.isConfused = false;
        participant.isInfatuated = false;
        var rawName = pokemon.pokemonName.Replace("Foe ", "");
        pokemon.pokemonName = rawName;
    }
}
