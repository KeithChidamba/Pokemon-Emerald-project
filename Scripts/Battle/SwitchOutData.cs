using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SwitchOutData
{
    public int partyPosition;
    public int memberToSwapWith;
    public Battle_Participant participant;

    public SwitchOutData(int partyPosition,int memberToSwapWith,Battle_Participant participant)
    {
        this.partyPosition = partyPosition;
        this.memberToSwapWith = memberToSwapWith;
        this.participant = participant;
    }
}
