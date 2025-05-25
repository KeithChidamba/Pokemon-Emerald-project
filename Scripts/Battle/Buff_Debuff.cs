using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Buff_Debuff
{
    [SerializeField]public bool isAtLimit;
    [SerializeField]public string stat;
    [SerializeField]public int stage;

    public Buff_Debuff(string statName, int stage, bool isAtLimit)
    {
        stat = statName;
        this.stage = stage;
        this.isAtLimit = isAtLimit;
    }
}
