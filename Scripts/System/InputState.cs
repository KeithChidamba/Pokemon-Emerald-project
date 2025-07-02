using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class InputState
{
    public string stateName;
    public string stateNameLower;
    public InputStateHandler.Directional stateDirectional;
    public List<SelectableUI> selectableUis;
    public Action OnExit;
    public Action OnClose;
    public int currentSelectionIndex;
    public bool isSelecting;
    public bool displayingSelector;
    public GameObject selector;
    public int maxSelectionIndex;
    public GameObject mainViewUI;
    public bool isParentLayer;
    public bool canExit;
    public InputState(string stateName, bool isParent,GameObject mainView,InputStateHandler.Directional stateDirectional, List<SelectableUI> selectableUis
        ,GameObject selector,bool selecting,bool display,Action onClose,Action onExit,bool canExit)
    {
        isParentLayer = isParent;
        this.canExit = canExit;
        if (isParentLayer) mainViewUI = mainView;
        
        this.stateName = stateName;
        stateNameLower = stateName.ToLower();
        this.stateDirectional = stateDirectional; 
        this.selectableUis = selectableUis;
        currentSelectionIndex = 0;
        this.selector = selector;
        isSelecting = selecting;
        displayingSelector = display;
        OnExit = onExit;
        OnClose = onClose;
    }
}
