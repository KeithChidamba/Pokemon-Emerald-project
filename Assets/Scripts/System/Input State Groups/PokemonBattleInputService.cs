using System;

public class PokemonBattleInputService : IInputGroup
{
    private InputStateHandler _inputStateHandler;
    private BattleHandler _battleHandler;
    private DialogueHandler _dialogueHandler;
    private BattleParticipantKey _previousEnemySelection;
    
    public PokemonBattleInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
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
        //this state reset is necessary because this state persists
        //unlike [move selection] with is removed and recreated every time it's needed
        _inputStateHandler.ResetGridCoordinates();
        _inputStateHandler.currentState.currentSelectionIndex = 0;
        _inputStateHandler.currentState.persistOnExit = true;//for turn re-use logic
        _inputStateHandler.SetupFullBoxNavigation(4,2);
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
        return;
        void SelectEnemy(int indexChange)
        {
            _battleHandler.ResetEnemyColor();
            _battleHandler.SelectEnemy(indexChange,_previousEnemySelection);
        }
    }
}
