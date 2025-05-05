using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffData
{
    public readonly Pokemon Reciever;
    public readonly string StatName;
    public readonly bool IsIncreasing;
    public readonly int EffectAmount;
    public BuffDebuffData(Pokemon reciever, string statName, bool isIncreasing, int effectAmount)
    {
        Reciever = reciever;
        StatName = statName;
        IsIncreasing = isIncreasing;
        EffectAmount = effectAmount;
    }
}
