using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Buff_Debuff
{
    [SerializeField]public bool isAtLimit;
    [SerializeField]public string statName;
    public PokemonOperations.Stat stat;
    [SerializeField]public int stage;

    public Buff_Debuff(PokemonOperations.Stat stat, int stage, bool isAtLimit)
    {
        statName = NameDB.GetStatName(stat);
        this.stat = stat;
        this.stage = stage;
        this.isAtLimit = isAtLimit;
    }
}
