using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move_handler 
{
    public List<Move> All_moves;
    public static Move_handler instance;
    private void Awake()
    {
        /*if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }*/
        instance = new Move_handler();
    }
    public void Do_move(Turn turn)
    {
        Dialogue_handler.instance.Write_Info(turn.attacker_.Pokemon_name+" used "+turn.move_.Move_name+" on "+turn.victim_.Pokemon_name+"!","Battle info");
        Dialogue_handler.instance.Dialouge_off(1.2f);
        Battle_handler.instance.Invoke(nameof(Battle_handler.instance.Next_turn),1.3f);
        //async
        //damage pokemon
        //call appropriate move for move effect
    }
}
