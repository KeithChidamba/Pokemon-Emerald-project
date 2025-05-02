using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_event : ICommand
{
    private Action OnEventTriggered;
    public bool Condition;
    public Battle_event(Action eventmethod, bool condition)
    {
        OnEventTriggered+=eventmethod;
        Condition = condition;
    }
    public void Execute()
    {
        OnEventTriggered?.Invoke();
    }
    public void Undo()
    {

    }
}
