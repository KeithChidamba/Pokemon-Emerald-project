using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSequenceEvent
{
    private Action<Move,BattleParticipant,BattleParticipant> _onEventTriggered;
    public bool Condition;
    public BattleSequenceEvent(Action<Move,BattleParticipant,BattleParticipant> onEventMethod, bool condition)
    {
        _onEventTriggered+=onEventMethod;
        Condition = condition;
    }
    public void Execute(Move move,BattleParticipant attacker, BattleParticipant victim)
    {
        _onEventTriggered?.Invoke(move,attacker,victim);
    }
}
