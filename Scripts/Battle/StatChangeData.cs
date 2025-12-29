using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatChangeData
{
    public StatChangeability Changeability;
    public int EffectDuration;

    public StatChangeData(StatChangeability changeability, int effectDuration)
    {
        Changeability = changeability;
        EffectDuration = effectDuration;
    }
}

public enum StatChangeability{ImmuneToIncrease,ImmuneToDecrease}
