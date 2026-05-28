using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypingInterfaceInputService:IInputGroup
{
    private Game_ui_manager _gameUIHandler;
    private TypingInterfaceHandler _typingInterfaceHandler;
    private InputStateHandler _inputStateHandler;
    
    public TypingInterfaceInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _typingInterfaceHandler = container.Resolve<TypingInterfaceHandler>();
    }
    public void DetermineOperation()
    {
        _inputStateHandler.OnFullBoxNavigation += CheckBoundaryEnter;
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.TypingInterfaceNavigation => TypingFullBoxNavigation,
            InputStateName.TypingInterfaceOptions => OptionsNavigation,
            _ => null
        };
        stateMethod?.Invoke();
    }

    private void TypingFullBoxNavigation()
    {
        _typingInterfaceHandler.optionSelector.SetActive(false);
        _inputStateHandler.currentNumBoxElements = _typingInterfaceHandler.currentMaxBoxElements;
        _inputStateHandler.currentBoxCapacity = _typingInterfaceHandler.currentMaxBoxElements;
        _inputStateHandler.numBoxColumns = _typingInterfaceHandler.GetColumnCount();
        _inputStateHandler.numBoxRows = _typingInterfaceHandler.currentMaxBoxElements / _inputStateHandler.numBoxColumns;
        
        _inputStateHandler.OnInputLeft += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Horizontal,-1);
        _inputStateHandler.OnInputRight += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Horizontal,1);
        _inputStateHandler.OnInputUp += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Vertical,-1);
        _inputStateHandler.OnInputDown += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Vertical,1);
    }

    private void CheckBoundaryEnter(int change,bool isVertical)
    {
        //if user moves to the right at the box boundary
        //by design boundary is always on the right 
        if (_inputStateHandler.boxCoordinates[1]==_inputStateHandler.numBoxColumns-1 && change>0 && !isVertical)
        {
            _inputStateHandler.OnSelectionIndexChanged += SwitchToOptions;
        }
        
        void SwitchToOptions(int index)
        {
            _inputStateHandler.RemoveTopInputLayer(false);
            _typingInterfaceHandler.InterfaceOptionsNavigation();
        }
    }
    private void OptionsNavigation()
    {
        _typingInterfaceHandler.characterSelector.SetActive(false);
        _inputStateHandler.OnInputLeft += _typingInterfaceHandler.TypingInterfaceNavigation;
    }
}
