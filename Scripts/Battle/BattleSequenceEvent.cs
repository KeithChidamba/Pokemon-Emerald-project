using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSequenceEvent
{
    private Action<Move,Battle_Participant,Battle_Participant> _onEventTriggered;
    public bool Condition;
    public BattleSequenceEvent(Action<Move,Battle_Participant,Battle_Participant> onEventMethod, bool condition)
    {
        _onEventTriggered+=onEventMethod;
        Condition = condition;
    }
    public void Execute(Move move,Battle_Participant attacker, Battle_Participant victim)
    {
        _onEventTriggered?.Invoke(move,attacker,victim);
    }
}
