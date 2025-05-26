using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Battle_Data:MonoBehaviour
{
    public float attack;
    public float defense;
    public float spAtk;
    public float spDef;
    public float speed;
    private Battle_Participant _participant;
    private void Start()
    {
        _participant = GetComponent<Battle_Participant>();
    }
    public void SaveActualStats()
    {
        attack = _participant.pokemon.attack;
        spAtk = _participant.pokemon.specialAttack;
        defense = _participant.pokemon.defense;
        spDef = _participant.pokemon.specialDefense;
        speed = _participant.pokemon.speed;
    }
    public void LoadActualStats()
    {
        _participant.pokemon.attack=attack;
        _participant.pokemon.specialAttack=spAtk;
        _participant.pokemon.defense=defense;
        _participant.pokemon.specialDefense=spDef;
        _participant.pokemon.speed=speed;
    }
    public void ResetBattleState(Pokemon pokemon,bool justLeveledUp)
    {
        pokemon.accuracy = 100;
        pokemon.evasion = 100;
        pokemon.critChance = 6.25f;
        pokemon.buffAndDebuffs.Clear();
        if (justLeveledUp) return;
        pokemon.canAttack = true;
        pokemon.immuneToStatReduction = false;
        pokemon.canBeDamaged = true;
        pokemon.isFlinched = false;

        var rawName = pokemon.pokemonName.Replace("Foe ", "");
        pokemon.pokemonName = rawName;
    }
}
