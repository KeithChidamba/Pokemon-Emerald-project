using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonDetailsInputService: IInputGroup
{
    private PokemonDetailsHandler _pokemonDetailsHandler;
    private InputStateHandler _inputStateHandler;
    
    public PokemonDetailsInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _pokemonDetailsHandler = container.Resolve<PokemonDetailsHandler>();
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
}
