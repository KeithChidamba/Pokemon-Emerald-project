using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turn
{
     public Battle_Participant attacker_;
     public Battle_Participant victim_;
     public Move move_;

     public Turn(Move move,Battle_Participant attacker,Battle_Participant victim)
     {
          move_ = move;
          attacker_ = attacker;
          victim_ = victim;
     }
}
