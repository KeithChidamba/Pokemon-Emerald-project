using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatChangeTransitData
{
    public BattleParticipant receiver;
    public  Stat stat;
    public bool isIncreasing;
    public int effectAmount;
    
    public StatChangeTransitData(BattleParticipant receiver,  Stat stat, bool isIncreasing, int effectAmount)
    {
        this.receiver = receiver;
        this.stat = stat;
        this.isIncreasing = isIncreasing;
        this.effectAmount = effectAmount;
    }
}
