using System;
using UnityEngine;

public class TypingInterfaceInputService:IInputGroup
{
    private TypingInterfaceHandler _typingInterfaceHandler;
    private InputStateHandler _inputStateHandler;
    
    public TypingInterfaceInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
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
        _inputStateHandler.SetupFullBoxNavigation(
            _typingInterfaceHandler.currentMaxBoxElements,
            _typingInterfaceHandler.currentMaxBoxElements, 
          _typingInterfaceHandler.GetColumnCount());
        
        _inputStateHandler.OnSelectionIndexChanged += _typingInterfaceHandler.SetCurrentCharacterIndex;
        InputSourceHandler.OnInputPressed += CheckQuickTypingAction;
        _typingInterfaceHandler.OnCharacterCaptured += SwapOnInputLimit;
        return;
        
        void SwapOnInputLimit(int index)
        {
            //auto-trigger input finalization
            if(index == _typingInterfaceHandler.MaxCharacterLength)
            {
                QuickSwapToLastOption();
            }
        }
        void CheckQuickTypingAction(ControlEvent e)
        {
            if (e==ControlEvent.Exit)//shortcut to remove last typed character
            {
                _typingInterfaceHandler.ResetCharacterValue();
            }
            if (e==ControlEvent.OpenMenu)//shortcut to finalize input
            {
                QuickSwapToLastOption();
            }
        }
        void QuickSwapToLastOption()
        {
            _typingInterfaceHandler.OnCharacterCaptured -= SwapOnInputLimit;
            _inputStateHandler.OnStateLoaded += SelectLastOption;
            InputSourceHandler.OnInputPressed -= CheckQuickTypingAction;
            SwitchToOptions();
            void SelectLastOption(InputState newState)
            {
                _inputStateHandler.OnStateLoaded -= SelectLastOption;
                _inputStateHandler.SetSelectionIndex(_inputStateHandler.currentState.maxSelectableIndex);
                _inputStateHandler.UpdateSelectorUi();
            }
        }
    }

    private void CheckBoundaryEnter(int indexChange,bool isVertical)
    {
        //if user moves to the right at the box boundary
        //by design boundary is always on the right 
        var movingRight = indexChange > 0 && !isVertical;
        var atBoundary = _inputStateHandler.GetCoordinate(false) == _typingInterfaceHandler.GetColumnCount() - 1;
        if (atBoundary && movingRight)
        {
            _inputStateHandler.OnSelectionIndexChanged += (index) => SwitchToOptions();
        }
    }
    private void SwitchToOptions()
    {
        _inputStateHandler.ResetGridUi(InputStateName.TypingInterfaceNavigation);
        _typingInterfaceHandler.InterfaceOptionsNavigation();
    }
    private void OptionsNavigation()
    {
        _typingInterfaceHandler.characterSelector.SetActive(false);
        _inputStateHandler.OnInputLeft += _typingInterfaceHandler.TypingInterfaceNavigation;
    }
}
