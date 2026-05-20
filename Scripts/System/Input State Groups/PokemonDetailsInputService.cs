using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonDetailsInputService
{
    private Pokemon_Details _pokemonDetailsHandler;
    private InputStateHandler _inputStateHandler;
    private PokemonOperations _pokemonOperations;
    public PokemonDetailsInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _pokemonDetailsHandler = container.Resolve<Pokemon_Details>();
        _pokemonOperations = container.Resolve<PokemonOperations>();
    }
    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PokemonDetails => SetupPokemonDetails,
            _ => null
        };
        stateMethod?.Invoke();
    }
    private void SetupPokemonDetails()
    {
        _inputStateHandler.OnInputLeft += _pokemonDetailsHandler.PreviousPage;
        _inputStateHandler.OnInputRight += _pokemonDetailsHandler.NextPage;
        _inputStateHandler.OnInputUp += ()=>_pokemonDetailsHandler.ChangePokemon(-1);
        _inputStateHandler.OnInputDown += ()=>_pokemonDetailsHandler.ChangePokemon(1);
    }

    public void AllowMoveUiNavigation()
    {
        var moveSelectables = new List<SelectableUI>();
        for (var i = 0; i < _pokemonDetailsHandler.currentPokemon.moveSet.Count; i++)
        {
            moveSelectables.Add(new(_pokemonDetailsHandler.moveNamesText[i].gameObject,
                () => _pokemonDetailsHandler.SelectMove(_inputStateHandler.currentState.currentSelectionIndex), true));
        }

        Action onExit = null;
        if (_pokemonDetailsHandler.changingMoveData) 
            onExit = () => _pokemonDetailsHandler.OnMoveSelected?.Invoke(-1);

        if (_pokemonDetailsHandler.learningMove)
            onExit = ()=> _inputStateHandler.OnStateRemoved += RemoveDetailsInputStates;
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonDetailsMoveSelection,InputStateGroup.PokemonDetails,
            stateDirection:InputDirection.Vertical,selectableUis:moveSelectables, 
            selector:_pokemonDetailsHandler.moveSelector, selecting:true, display:true,onExit:onExit));
    }
    void RemoveDetailsInputStates(InputState state)
    {
        if (state.stateName != InputStateName.PokemonDetailsMoveSelection) return;
        _inputStateHandler.OnStateRemoved -= RemoveDetailsInputStates;
        //if started learning but rejected it on move selection screen
        _pokemonOperations.SkipMove();
        _inputStateHandler.ResetGroupUi(InputStateGroup.PokemonDetails);
    }

}
