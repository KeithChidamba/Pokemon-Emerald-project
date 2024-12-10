using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turn
{
     public Pokemon attacker_;
     public Pokemon victim_;
     public Move move_;

     public Turn(Move move,Pokemon attacker,Pokemon victim)
     {
          move_ = move;
          attacker_ = attacker;
          victim_ = victim;
     }
}
