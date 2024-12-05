using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move_handler : MonoBehaviour
{
    public List<Move> All_moves;
    public Battle_handler battle;
    public Dialogue_handler dialogue;
    public void Do_move(Move move,Pokemon attacker,Pokemon victim)
    {
        dialogue.Write_Info(attacker.Pokemon_name+" used "+move.Move_name+"!","Battle info");
        //
        //damage pokemon
        //call appropriate move for move effect
    }
}
