using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSequenceEvent
{
    private Action _onEventTriggered;
    public bool Condition;
    public BattleSequenceEvent(Action onEventMethod, bool condition)
    {
        _onEventTriggered+=onEventMethod;
        Condition = condition;
    }
    public void Execute()
    {
        _onEventTriggered?.Invoke();
    }
}
