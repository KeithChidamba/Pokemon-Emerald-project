using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Turn
{
     public int attackerIndex;
     public int victimIndex;
     public long victimID;
     public long attackerID;
     public Move move;
     public TurnUsage turnUsage;
     public SwitchOutData switchData;
     public bool isCancelled;
     public bool turnExecuted;
     public Turn(TurnUsage turnUsage,Move move = null,
          int attacker = 0,int victim = 0, long attackerID = 0,long victimID = 0)
     {
          this.move = move;
          attackerIndex = attacker;
          victimIndex = victim;
          this.attackerID= attackerID;
          this.victimID = victimID;
          this.turnUsage = turnUsage;
     }
     public Turn(Turn copyRequest)
     {
          move = copyRequest.move;
          attackerIndex = copyRequest.attackerIndex;
          victimIndex = copyRequest.victimIndex;
          attackerID= copyRequest.attackerID;
          victimID = copyRequest.victimID;
     }
}

public enum TurnUsage{Attack,SwitchOut}
