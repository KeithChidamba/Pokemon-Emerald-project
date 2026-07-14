using System;
using UnityEngine;

public class PokemonBattleInputService : IInputGroup
{
    private InputStateHandler _inputStateHandler;
    private Battle_handler _battleHandler;
    private Dialogue_handler _dialogueHandler;
    private BattleParticipantKey _previousEnemySelection;
    
    public PokemonBattleInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
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
        _inputStateHandler.SetupFullBoxNavigation(4,4,2);
    }
    
    private void SetupMoveSelection()
    {
        _dialogueHandler.EndDialogue();
        ref InputState currentState = ref _inputStateHandler.currentState;
        currentState.currentSelectionIndex = 0;
        _inputStateHandler.SetupDynamicBoxNavigation(currentState.maxSelectableIndex+1,4,2);
        _inputStateHandler.OnSelectionIndexChanged += _battleHandler.SelectMove;
    }

    private void SetupEnemySelection()
    {
        _battleHandler.OnEnemySelected += (index) => _previousEnemySelection = index;
        _battleHandler.SelectEnemy(0);//select default enemy
        
        _inputStateHandler.OnInputLeft += ()=> SelectEnemy(-1);
        _inputStateHandler.OnInputRight += () => SelectEnemy(1);
        
        void SelectEnemy(int indexChange)
        {
            _battleHandler.ResetEnemyColor();
            _battleHandler.SelectEnemy(indexChange,_previousEnemySelection);
        }
    }
}
