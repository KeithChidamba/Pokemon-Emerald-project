using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SwitchOutData
{
    public int PartyPosition;
    public int MemberToSwapWith;
    public Battle_Participant Participant;
    public bool IsPlayer;
    public SwitchOutData(int partyPosition,int memberToSwapWith,Battle_Participant participant,bool isPlayer = true)
    {
        PartyPosition = partyPosition;
        MemberToSwapWith = memberToSwapWith;
        Participant = participant;
        IsPlayer = isPlayer;
    }
}
