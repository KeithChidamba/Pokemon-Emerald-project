using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Turn
{
     public BattleParticipantKey attackerKey;
     public BattleParticipantKey victimKey;
     public long victimID;
     public long attackerID;
     public Move move;
     public TurnUsage turnUsage;
     public SwitchOutData switchData;
     public bool isCancelled;
     public bool turnExecuted;
     public Turn(TurnUsage turnUsage,Move move = null,
          BattleParticipantKey attackerKey = 0,BattleParticipantKey victimKey = 0,
          long attackerID = 0,long victimID = 0)
     {
          this.move = move;
          this.attackerKey = attackerKey;
          this.victimKey = victimKey;
          this.attackerID= attackerID;
          this.victimID = victimID;
          this.turnUsage = turnUsage;
     }
     public Turn(Turn copyRequest)
     {
          move = copyRequest.move;
          attackerKey = copyRequest.attackerKey;
          victimKey = copyRequest.victimKey;
          attackerID= copyRequest.attackerID;
          victimID = copyRequest.victimID;
     }
}

public enum TurnUsage{Attack,SwitchOut,UseStruggle}
