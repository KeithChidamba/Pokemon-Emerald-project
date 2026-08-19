using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TrapDataInfo : DynamicAdditionalInfo
{
    public enum TrapType
    {
        PersistentFromMove,
        RandomDurationFromMove,
        PersistentFromAbility
    }
    
    public TrapType trapType;

    [HideInInspector]public int trapDuration;
    
    public int maxTrapDuration;
    public int minTrapDuration;
    public string onTrapMessage;
    public string onHitMessage;
    public string onFreeMessage;

    public void SetRandomDuration()
    {
        var numTurnsOfTrap = Utility.RandomRange(minTrapDuration, maxTrapDuration + 1);
        trapDuration = numTurnsOfTrap;
    }
}
