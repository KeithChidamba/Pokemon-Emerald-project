using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_event : ICommand
{
    private Action OnEventTriggered;
    public bool Condition;
    public float duration;
    public Battle_event(Action eventmethod, bool condition,float duration_)
    {
        OnEventTriggered+=eventmethod;
        Condition = condition;
        duration = duration_;
    }
    public void Execute()
    {
        OnEventTriggered?.Invoke();
    }
    public void Undo()
    {

    }
}
