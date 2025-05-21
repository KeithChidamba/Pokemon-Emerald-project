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
    public string pokemonName;
    private Battle_Participant _participant;
    private void Start()
    {
        _participant = GetComponent<Battle_Participant>();
    }
    public void SaveActualStats()
    {
        pokemonName = _participant.pokemon.pokemonName;
        attack = _participant.pokemon.attack;
        spAtk = _participant.pokemon.specialAttack;
        defense = _participant.pokemon.defense;
        spDef = _participant.pokemon.specialDefense;
        speed = _participant.pokemon.speed;
    }
    public void LoadActualStats()
    {
        _participant.pokemon.pokemonName = pokemonName;
        _participant.pokemon.attack=attack;
        _participant.pokemon.specialAttack=spAtk;
        _participant.pokemon.defense=defense;
        _participant.pokemon.specialDefense=spDef;
        _participant.pokemon.speed=speed;
    }
    public void ResetBattleState(Pokemon pokemon,bool notBattling)
    {
        pokemon.accuracy = 100;
        pokemon.evasion = 100;
        pokemon.critChance = 6.25f;
        pokemon.canAttack = notBattling;
        pokemon.immuneToStatReduction = !notBattling;
        pokemon.canBeDamaged = true;
        pokemon.isFlinched = false;
        pokemon.buffAndDebuffs.Clear();
    }
}
