using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonPartyInputService: IInputGroup
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private InputStateHandler _inputStateHandler;
    
    public PokemonPartyInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
    }

    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PokemonPartyItemUsage => _pokemonPartyHandler.UpdateHealthBarColors,
            InputStateName.PokemonPartyNavigation => _pokemonPartyHandler.UpdateHealthBarColors,

            _ => null
        };
        stateMethod?.Invoke();
    }
}
