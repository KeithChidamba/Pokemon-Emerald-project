using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Turn
{
     [SerializeField]public int attackerIndex;
     [SerializeField]public int victimIndex;
     [SerializeField]public Move move_;
     
     public Turn(Move move,int attacker,int victim)
     {
          move_ = move;
          attackerIndex = attacker;
          victimIndex = victim;
     }
}
