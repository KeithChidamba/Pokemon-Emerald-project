using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnCoolDown
{
    public int NumTurns;
    public int VictimIndex;
    public string Message;
    public Move MoveToExecute;
    public bool DisplayMessage;
    public TurnCoolDown(Move move, int numTurns, int victimIndex, string message, bool display = true)
    {
        MoveToExecute = move;
        NumTurns = numTurns;
        Message = message;
        VictimIndex = victimIndex;
        DisplayMessage = display;
    }

    public void StoreDamage(float damage)
    {
       MoveToExecute.moveDamage += damage;
    }
}
