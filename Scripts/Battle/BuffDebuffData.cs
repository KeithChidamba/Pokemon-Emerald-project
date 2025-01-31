using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffData
{
    public Pokemon Reciever;
    public string StatName;
    public bool isIncreasing;
    public int EffectAmount;
    public BuffDebuffData(Pokemon reciever, string statName, bool is_Increasing, int effectAmount)
    {
        Reciever = reciever;
        StatName = statName;
        isIncreasing = is_Increasing;
        EffectAmount = effectAmount;
    }
}
