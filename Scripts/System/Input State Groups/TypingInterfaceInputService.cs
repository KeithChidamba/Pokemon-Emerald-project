using System;

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
        
    }

    private void CheckBoundaryEnter(int indexChange,bool isVertical)
    {
        //if user moves to the right at the box boundary
        //by design boundary is always on the right 
        var movingRight = indexChange > 0 && !isVertical;
        var atBoundary = _inputStateHandler.GetCoordinate(false) == _typingInterfaceHandler.GetColumnCount() - 1;
        if (atBoundary && movingRight)
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
