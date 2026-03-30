using System;
using System.Collections;
using System.Collections.Generic;

public class GameSettingsInputService
{
    private InputStateHandler _inputStateHandler;

    public GameSettingsInputService(Container container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
    }

    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.GameSettingsNavigation => NavigateSetting,
            InputStateName.GameSettingOptionsNavigation => NavigateGameSettingOptions,
            _ => null
        };
        stateMethod?.Invoke();
    }

    private void NavigateSetting()
    {

    }
    
    private void NavigateGameSettingOptions()
    {

    }
}