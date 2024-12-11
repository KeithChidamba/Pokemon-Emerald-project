using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pkm_Use_Move : ICommand
{
    public Turn _turn;
    public Pkm_Use_Move(Turn t)
    {
        _turn  = new Turn(t.move_,t.attacker_,t.victim_);
    }
    public void Execute()
    {
        Move_handler.Do_move(_turn);
    }
    public void Undo()
    {
        _turn = null;
    }
    
}
