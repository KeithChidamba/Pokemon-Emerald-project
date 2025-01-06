using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_Data:MonoBehaviour
{
    public float Attack;
    public float Defense;
    public float SP_ATK;
    public float SP_DEF;
    public float speed;
    [SerializeField] private Battle_Participant participant;
    private void Start()
    {
        participant = GetComponent<Battle_Participant>();
    }
    public void save_stats()
    {
        Attack = participant.pokemon.Attack;
        SP_ATK = participant.pokemon.SP_ATK;
        Defense = participant.pokemon.Defense;
        SP_DEF = participant.pokemon.SP_DEF;
        speed = participant.pokemon.speed;
    }
    public void Load_Stats()
    {
        participant.pokemon.Attack=Attack;
        participant.pokemon.SP_ATK=SP_ATK;
        participant.pokemon.Defense=Defense;
        participant.pokemon.SP_DEF=SP_DEF;
        participant.pokemon.speed=speed;
        save_stats();
    }
    public void Reset_Battle_state(Pokemon pokemon,bool NotBattling)
    {
        pokemon.Accuracy = 100;
        pokemon.Evasion = 100;
        pokemon.crit_chance = 6.25f;
        if(NotBattling)
            pokemon.canAttack = true;
        pokemon.CanBeDamaged = true;
        pokemon.isFlinched = false;
        pokemon.Buff_Debuffs.Clear();
    }
}
