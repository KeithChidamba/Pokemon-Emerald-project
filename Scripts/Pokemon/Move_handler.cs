using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move_handler 
{
    public static void Do_move(Turn turn)
    {
        Dialogue_handler.instance.Write_Info(turn.attacker_.Pokemon_name+" used "+turn.move_.Move_name+" on "+turn.victim_.Pokemon_name+"!","Battle info");
        Dialogue_handler.instance.Dialouge_off(1.2f);
        
        //async
        //damage pokemon
        //call appropriate move for move effect
    }
}
