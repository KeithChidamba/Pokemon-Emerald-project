using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class StatChangeData
{
    [SerializeField]public bool isAtLimit;
    [SerializeField]public string statName;
    public Stat stat;
    [SerializeField]public int stage;

    public StatChangeData(Stat stat, int stage, bool isAtLimit = false)
    {
        statName = NameDB.GetStatName(stat);
        this.stat = stat;
        this.stage = stage;
        this.isAtLimit = isAtLimit;
    }
}
