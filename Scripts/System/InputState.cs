using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class InputState
{
    public string stateName;
    public string[] stateDirectionals;
    public List<SelectableUI> SelectableUI;
    public int currentSelectionIndex;
    public bool isSelecting;
    public bool displayingSelector;
    public GameObject selector;
    public int maxSelectionIndex;
    public InputState(string stateName, string[] stateDirectionals, List<SelectableUI> SelectableUI ,GameObject selector,bool selecting,bool display)
    {
        this.stateName = stateName;
        this.stateDirectionals = stateDirectionals; 
        this.SelectableUI = SelectableUI;
        currentSelectionIndex = 0;
        this.selector = selector;
        isSelecting = selecting;
        displayingSelector = display;
    }
}
