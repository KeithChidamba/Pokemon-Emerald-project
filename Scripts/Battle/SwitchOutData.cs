using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SwitchOutData
{
    public int PartyPosition;
    public int MemberToSwapWith;
    public Battle_Participant Participant;

    public SwitchOutData(int partyPosition,int memberToSwapWith,Battle_Participant participant)
    {
        PartyPosition = partyPosition;
        MemberToSwapWith = memberToSwapWith;
        Participant = participant;
    }
}
