using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InputState
{
    public InputStateName stateName;
    public InputStateGroup stateGroup;
    public InputDirection stateDirection;
    public List<SelectableUI> selectableUis;
    public Action onExit;
    public Action onClose;
    public Func<bool> updateExitStatus;
    public int currentSelectionIndex;
    public bool isSelecting;
    public bool displayingSelector;
    public GameObject selector;
    public int maxSelectableIndex;
    public GameObject mainViewUI;
    public bool isParentLayer;
    public bool canExit;
    public bool canManualExit;
    public bool persistOnExit;
    public bool displayCloseTransition;
    public bool displayOpenTransition;
    public InputState(
        InputStateName stateName,
        InputStateGroup group,
        bool isParent = false,
        GameObject mainView = null,
        InputDirection stateDirection = InputDirection.None,
        List<SelectableUI> selectableUis = null,
        GameObject selector = null,
        bool selecting = false,
        bool display = false,
        Action onClose = null,
        Action onExit = null,
        Func<bool> updateExit = null,
        bool canExit = true,
        bool canManualExit = true,
        bool displayOpenTransition =false,
        bool displayCloseTransition =false
    )
    {
        this.stateName = stateName;
        stateGroup = group;
        isParentLayer = isParent;
        mainViewUI = isParent ? mainView : null;
        this.stateDirection = stateDirection;
        this.selectableUis = selectableUis;
        this.selector = selector;
        isSelecting = selecting;
        displayingSelector = display;
        currentSelectionIndex = 0;
        this.onClose = onClose;
        this.onExit = onExit;
        
        this.canManualExit = canManualExit;
        
        if (!canExit)
        {
            this.canManualExit = false;
        }
        
        this.canExit = canExit;

        this.displayOpenTransition = displayOpenTransition;

        this.displayCloseTransition = displayOpenTransition || displayCloseTransition;
        
        updateExitStatus = updateExit;
    }
    
}
