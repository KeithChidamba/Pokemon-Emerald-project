using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_event
{
    private Action OnEventTriggered;
    public bool Condition;
    public Battle_event(Action eventMethod, bool condition)
    {
        OnEventTriggered+=eventMethod;
        Condition = condition;
    }
    public void Execute()
    {
        OnEventTriggered?.Invoke();
    }
}
