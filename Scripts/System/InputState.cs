using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class InputState
{
    public string stateName;
    public string[] stateDirectionals;
    public List<SelectableUI> selectableUis;
    public Action OnExit;
    public int currentSelectionIndex;
    public bool isSelecting;
    public bool displayingSelector;
    public GameObject selector;
    public int maxSelectionIndex;
    public InputState(string stateName, string[] stateDirectionals, List<SelectableUI> selectableUis ,GameObject selector,bool selecting,bool display,Action onExit)
    {
        this.stateName = stateName;
        this.stateDirectionals = stateDirectionals; 
        this.selectableUis = selectableUis;
        currentSelectionIndex = 0;
        this.selector = selector;
        isSelecting = selecting;
        displayingSelector = display;
        OnExit = onExit;
    }
}
