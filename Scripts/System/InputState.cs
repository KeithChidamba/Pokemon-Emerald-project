using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class InputState
{
    public string stateName;
    public string[] stateDirectionals;
    public List<Action> InputEvents;
    public int currentSelectionIndex;
    public bool isSelecting;
    public int maxSelectionIndex;
    public InputState(string stateName, string[] stateDirectionals, List<Action> inputEvents,bool selecting)
    {
        this.stateName = stateName;
        this.stateDirectionals = stateDirectionals; 
        InputEvents = inputEvents;
        currentSelectionIndex = -1;
        isSelecting = selecting;
    }
}
