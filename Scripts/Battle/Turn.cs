using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Turn
{
     [SerializeField]public Battle_Participant attacker_;
     [SerializeField]public Battle_Participant victim_;
     [SerializeField]public Move move_;

     public Turn(Move move,Battle_Participant attacker,Battle_Participant victim)
     {
          move_ = move;
          attacker_ = attacker;
          victim_ = victim;
     }
}
