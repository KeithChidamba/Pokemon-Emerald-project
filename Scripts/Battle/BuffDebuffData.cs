using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffData
{
    public readonly Battle_Participant Receiver;
    public readonly string StatName;
    public readonly bool IsIncreasing;
    public readonly int EffectAmount;
    public BuffDebuffData(Battle_Participant receiver, string statName, bool isIncreasing, int effectAmount)
    {
        Receiver = receiver;
        StatName = statName;
        IsIncreasing = isIncreasing;
        EffectAmount = effectAmount;
    }
}
