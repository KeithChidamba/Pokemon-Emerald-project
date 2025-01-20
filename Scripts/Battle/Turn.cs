using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Turn
{
     [SerializeField]public int attackerIndex;
     [SerializeField]public int victimIndex;
     [SerializeField]public string victimID;
     [SerializeField]public string attackerID;
     [SerializeField]public Move move_;
     
     public Turn(Move move,int attacker,int victim, string attackerID_,string victimID_)
     {
          move_ = move;
          attackerIndex = attacker;
          victimIndex = victim;
          attackerID= attackerID_;
          victimID = victimID_;
     }
}
