using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_event
{
    private Action _onEventTriggered;
    public bool Condition;
    public Battle_event(Action onEventMethod, bool condition)
    {
        _onEventTriggered+=onEventMethod;
        Condition = condition;
    }
    public void Execute()
    {
        _onEventTriggered?.Invoke();
    }
}
