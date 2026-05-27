using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokemonPartyInputService: IInputGroup
{
    private Bag _playerBagHandler;
    private Game_ui_manager _gameUIHandler; 
    private Pokemon_party _pokemonPartyHandler;
    private InputStateHandler _inputStateHandler;
    
    public PokemonPartyInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _playerBagHandler = container.Resolve<Bag>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
    }

    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PokemonPartyItemUsage => UpdateHealthBarColors,
            InputStateName.PokemonPartyNavigation => UpdateHealthBarColors,

            _ => null
        };
        stateMethod?.Invoke();
    }

    public void UpdateHealthBarColors()
    {
        for (var i = 0;i<_pokemonPartyHandler.numMembers;i++)
        {
            PokemonOperations.UpdateHealthPhase(_pokemonPartyHandler.party[i], 
                _pokemonPartyHandler.memberCards[i].hpSliderImage);
        }
    }
    public void PokemonPartyOptions()
    {
        var partyOptionsSelectables = new List<SelectableUI>
        {
            new(_pokemonPartyHandler.partyOptions[0]
                , ()=>_gameUIHandler.ViewPartyPokemonDetails(
                    _pokemonPartyHandler.party[_pokemonPartyHandler.selectedMemberNumber - 1]), true),
            new(_pokemonPartyHandler.partyOptions[1]
                , () => _pokemonPartyHandler.SelectMemberToBeSwapped(_pokemonPartyHandler.selectedMemberNumber)
                , true),
            new(_pokemonPartyHandler.partyOptions[2]
                , _playerBagHandler.OpenBagToGiveItem
                ,!_pokemonPartyHandler.party[_pokemonPartyHandler.selectedMemberNumber - 1].hasItem),
            new(_pokemonPartyHandler.partyOptions[3]
                , () => _playerBagHandler.TakeItem(_pokemonPartyHandler.selectedMemberNumber)
                ,_pokemonPartyHandler.party[_pokemonPartyHandler.selectedMemberNumber - 1].hasItem)
        };
        partyOptionsSelectables.RemoveAll(s=>!s.canBeSelected);
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonPartyOptions,
            InputStateGroup.PokemonParty, stateDirection:InputDirection.Vertical, selectableUis:partyOptionsSelectables
            ,selector:_pokemonPartyHandler.optionSelector,selecting:true,display:true
            ,onClose:_pokemonPartyHandler.ClearSelectionUI,onExit:_pokemonPartyHandler.ClearSelectionUI));
        _inputStateHandler.currentState.selector.SetActive(true);
    }
}
