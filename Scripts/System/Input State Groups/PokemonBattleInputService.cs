using System;
using UnityEngine;

public class PokemonBattleInputService 
{
    private InputStateHandler _inputStateHandler;
    private Battle_handler _battleHandler;

    public PokemonBattleInputService(Container container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
       
        _battleHandler = container.Resolve<Battle_handler>();
    }
    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PokemonBattleOptions => SetupBattleOptions,
            InputStateName.PokemonBattleMoveSelection => SetupMoveSelection,
            InputStateName.PokemonBattleEnemySelection => SetupEnemySelection,
            _ => null
        };
        stateMethod?.Invoke();
    }
    
    private void SetupBattleOptions()
    {
        _inputStateHandler.currentState.persistOnExit = true;
        _inputStateHandler.currentState.currentSelectionIndex = 0;
        _inputStateHandler.currentNumBoxElements = 4;
        _inputStateHandler.currentBoxCapacity = 4;
        _inputStateHandler.numBoxColumns = 2;
        _inputStateHandler.SetRowRemainder();
        _inputStateHandler.OnInputLeft += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Horizontal,-1);
        _inputStateHandler.OnInputRight += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Horizontal,1);
        _inputStateHandler.OnInputUp += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Vertical,-2);
        _inputStateHandler.OnInputDown += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Vertical,2);
    }
    
    private void SetupMoveSelection()
    {
        _battleHandler.battleParticipants[_battleHandler.currentEnemyIndex]
            .pokemonImage.color = Color.HSVToRGB(0,0,100);//reset color if cancelled
        
        ref InputState currentState = ref _inputStateHandler.currentState;
        currentState.currentSelectionIndex = 0;
        _inputStateHandler.currentNumBoxElements = currentState.maxSelectionIndex+1;
        _inputStateHandler.currentBoxCapacity = 4;
        _inputStateHandler.numBoxColumns = 2;
        _inputStateHandler.SetRowRemainder();
        
        _inputStateHandler.OnInputLeft += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Horizontal,-1);
        _inputStateHandler.OnInputRight += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Horizontal,1);
        _inputStateHandler.OnInputUp += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Vertical,-2);
        _inputStateHandler.OnInputDown += ()=>_inputStateHandler.MoveCoordinatesDynamic(InputDirection.Vertical,2);
        
        _inputStateHandler.OnSelectionIndexChanged += _battleHandler.SelectMove;
    }

    private void SetupEnemySelection()
    {
        _battleHandler.SelectEnemy(3);
        _inputStateHandler.OnInputLeft += ()=> _battleHandler.SelectEnemy(-1);
        _inputStateHandler.OnInputRight += () => _battleHandler.SelectEnemy(1);
    }
}
