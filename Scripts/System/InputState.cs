using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InputState
{
    public InputStateName stateName;
    public InputStateGroup[] stateGroups;
    public InputDirection stateDirection;
    public List<SelectableUI> selectableUis;
    public Action OnExit;
    public Action OnClose;
    public Func<bool> UpdateExitStatus;
    public int currentSelectionIndex;
    public bool isSelecting;
    public bool displayingSelector;
    public GameObject selector;
    public int maxSelectionIndex;
    public GameObject mainViewUI;
    public bool isParentLayer;
    public bool canExit;
    public bool canManualExit;
    public bool persistOnExit;
    
    public InputState(
        InputStateName stateName,
        InputStateGroup[] groups,
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
        bool canManualExit = true
    )
    {
        this.stateName = stateName;
        stateGroups = groups;
        isParentLayer = isParent;
        mainViewUI = isParent ? mainView : null;
        this.stateDirection = stateDirection;
        this.selectableUis = selectableUis;
        this.selector = selector;
        isSelecting = selecting;
        displayingSelector = display;
        currentSelectionIndex = 0;
        OnClose = onClose;
        OnExit = onExit;
        
        this.canManualExit = canManualExit;
        
        if (!canExit)
        {
            this.canManualExit = false;
        }
        
        this.canExit = canExit;
       
        UpdateExitStatus = updateExit;
    }
    
}
