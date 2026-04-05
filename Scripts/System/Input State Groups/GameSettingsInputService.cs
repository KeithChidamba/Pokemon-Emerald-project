using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettingsInputService
{
    private InputStateHandler _inputStateHandler;
    private GameSettingsHandler _gameSettingsHandler;
    private Game_ui_manager _gameUIHandler;
    
    public GameSettingsInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _gameSettingsHandler = container.Resolve<GameSettingsHandler>();
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
        _inputStateHandler.OnSelectionIndexChanged += _gameSettingsHandler.SetCurrentSetting;

        _inputStateHandler.OnInputLeft += () => OpenCurrentSettingOptions(-1);
        _inputStateHandler.OnInputRight += () => OpenCurrentSettingOptions(1);

        void OpenCurrentSettingOptions(int change)
        {
            _gameSettingsHandler.SetCurrentOption(change);
            _gameUIHandler.ViewGameSettingsOptions();
        }
    }
    
    private void NavigateGameSettingOptions()
    {
        _inputStateHandler.OnInputUp += () => MoveToAdjacentSetting(-1);
        _inputStateHandler.OnInputDown += () => MoveToAdjacentSetting(1);

        _inputStateHandler.OnSelectionIndexChanged += _gameSettingsHandler.ReflectChangedSetting;
        _inputStateHandler.OnSelectionIndexChanged += (index) => _gameSettingsHandler.SetOptionTextColor(index);

        void MoveToAdjacentSetting(int change)
        {
            var state = _inputStateHandler.GetState(InputStateName.GameSettingsNavigation);
            state.currentSelectionIndex = Mathf.Clamp(state.currentSelectionIndex+change, 0, state.maxSelectionIndex);
            _gameSettingsHandler.SetCurrentSetting(state.currentSelectionIndex);
            _inputStateHandler.RemoveTopInputLayer(false);
        }
    }
}