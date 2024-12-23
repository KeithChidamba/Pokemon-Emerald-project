using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battle_event : ICommand
{
    private string event_name;
    public bool Condition;
    public float duration;
    public Battle_event(string eventName, bool condition,float duration_)
    {
        event_name = eventName;
        Condition = condition;
        duration = duration_;
    }
    public void Execute()
    {
        Move_handler.instance.Invoke(event_name,0f);
    }
    public void Undo()
    {

    }
}
