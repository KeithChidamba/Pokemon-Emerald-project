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
        pokemonName = _participant.pokemon.Pokemon_name;
        attack = _participant.pokemon.Attack;
        spAtk = _participant.pokemon.SP_ATK;
        defense = _participant.pokemon.Defense;
        spDef = _participant.pokemon.SP_DEF;
        speed = _participant.pokemon.speed;
    }
    public void LoadActualStats()
    {
        _participant.pokemon.Pokemon_name = pokemonName;
        _participant.pokemon.Attack=attack;
        _participant.pokemon.SP_ATK=spAtk;
        _participant.pokemon.Defense=defense;
        _participant.pokemon.SP_DEF=spDef;
        _participant.pokemon.speed=speed;
    }
    public void ResetBattleState(Pokemon pokemon,bool notBattling)
    {
        pokemon.Accuracy = 100;
        pokemon.Evasion = 100;
        pokemon.crit_chance = 6.25f;
        pokemon.canAttack = notBattling;
        pokemon.immuneToStatReduction = !notBattling;
        pokemon.CanBeDamaged = true;
        pokemon.isFlinched = false;
        pokemon.Buff_Debuffs.Clear();
    }
}
