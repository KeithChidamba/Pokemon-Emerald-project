using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Turn
{
     [SerializeField]public int attackerIndex;
     [SerializeField]public int victimIndex;
     [SerializeField]public long victimID;
     [SerializeField]public long attackerID;
     [SerializeField]public Move move;
     
     public Turn(Move move,int attacker,int victim, long attackerID,long victimID)
     {
          this.move = move;
          attackerIndex = attacker;
          victimIndex = victim;
          this.attackerID= attackerID;
          this.victimID = victimID;
     }
}
