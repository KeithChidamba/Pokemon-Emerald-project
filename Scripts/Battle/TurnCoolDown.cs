using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class TurnCoolDown
{
    public int NumTurns;
    public Turn turnData;
    public string Message;
    public bool DisplayMessage;
    public Battle_Participant participant;
    public bool isCoolingDown;
    public bool ExecuteTurn;
    private Move_handler _moveUsageHandler;
    
    public void UpdateCoolDown(int numTurns,Turn turn, string message=""
        , bool display = true,bool coolingDown=true)
    {
        turnData = turn;
        NumTurns = numTurns;
        Message = message;
        DisplayMessage = display;
        isCoolingDown = coolingDown;
    }
    public void UpdateCoolDown(int numTurns,Turn turn,Move_handler moveUsageHandler, string message=""
        , bool display = true,bool coolingDown=true)
    {
        UpdateCoolDown(numTurns, turn, message, display, coolingDown);
        _moveUsageHandler = moveUsageHandler;
    }
    public void ResetState()
    {
        _moveUsageHandler.OnDamageDeal -= StoreDamage;
        NumTurns = 0;
        Message = string.Empty;
        turnData = null;
        DisplayMessage = false;
        isCoolingDown = false;
        ExecuteTurn = false;
    }
    public void StoreDamage(float damage,Battle_Participant victim)
    {
        if (victim != participant) return;
       turnData.move.moveDamage += damage;
    }
}
