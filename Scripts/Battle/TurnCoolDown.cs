using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class TurnCoolDown
{
    public int NumTurns;
    public int VictimIndex;
    public string Message;
    public Move MoveToExecute;
    public bool DisplayMessage;
    public Battle_Participant participant;
    public bool isCoolingDown;
    public bool ExecuteTurn;
    public void UpdateCoolDown(Move move, int numTurns, int victimIndex, string message=""
        , bool display = true,bool coolingDown=true)
    {
        MoveToExecute = move;
        NumTurns = numTurns;
        Message = message;
        VictimIndex = victimIndex;
        DisplayMessage = display;
        isCoolingDown = coolingDown;
    }

    public void ResetState()
    {
        MoveToExecute = null;
        NumTurns = 0;
        Message = string.Empty;
        VictimIndex = 0;
        DisplayMessage = false;
        isCoolingDown = false;
        ExecuteTurn = false;
    }
    public void StoreDamage(float damage,Battle_Participant victim)
    {
        if (victim != participant) return;
       MoveToExecute.moveDamage += damage;
    }
}
