using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffData
{
    public readonly Battle_Participant Receiver;
    public readonly  Stat Stat;
    public readonly bool IsIncreasing;
    public readonly int EffectAmount;
    public BuffDebuffData(Battle_Participant receiver,  Stat stat, bool isIncreasing, int effectAmount)
    {
        Receiver = receiver;
        Stat = stat;
        IsIncreasing = isIncreasing;
        EffectAmount = effectAmount;
    }
}
