using System;
using System.Collections.Generic;
using UnityEngine;

public class PokemonStorageInputService: IInputGroup
{
    private PokemonStorageHandler _pokemonStorageHandler;
    private InputStateHandler _inputStateHandler;
    
    public PokemonStorageInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _pokemonStorageHandler = container.Resolve<PokemonStorageHandler>();
    }
    public void DetermineOperation()
    {
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PokemonStoragePartyNavigation => ()=>
            {
                _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.LoadPokemonData;
            },
            InputStateName.PokemonStorageDepositSelection =>()=>
            {
                _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.DisplayBoxCapacity;
            },
            InputStateName.PokemonStorageBoxNavigation => StorageFullBoxNavigation,
            InputStateName.PokemonStorageExit => SetupPokemonStorageExit,
            InputStateName.PokemonStorageBoxChange => BoxChangeStateNavigation,
            _ => null
        };
        stateMethod?.Invoke();
    }
    private void StorageFullBoxNavigation()
    {
        _inputStateHandler.SetupFullBoxNavigation(PokemonStorageHandler.BoxCapacity,PokemonStorageHandler.BoxColumns);
        
        _inputStateHandler.currentState.canExit = false;
        _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.LoadPokemonData;
        _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.UpdateBoxPosition;
        _inputStateHandler.OnFullBoxNavigation += AllowTopRowExit;
        return;
        void AllowTopRowExit(int change,bool isVertical)
        { 
            var movingUp = change<0 && isVertical;
            var atTopRow = _inputStateHandler.GetCoordinate(true) == 0;
            if (atTopRow && movingUp)
            {
                _inputStateHandler.OnSelectionIndexChanged += ExitTopRow;
                void ExitTopRow(int index)
                {
                    _inputStateHandler.ResetSpecificUi(InputStateName.PokemonStorageBoxNavigation);
                    _pokemonStorageHandler.ClearPokemonData();
                    PokemonStorageBoxChange();
                }
            }
        }
    }

    private void SetupPokemonStorageExit()
    {
        _pokemonStorageHandler.initialSelector.transform.rotation = Quaternion.Euler(0, 180, 180);
        _inputStateHandler.OnInputDown += PokemonStorageBoxChange;
    }

    private void BoxChangeStateNavigation()
    {
        _inputStateHandler.OnInputUp += SwitchToExit;
        _inputStateHandler.OnInputDown += SwitchToStorageBoxNavigation;
        _inputStateHandler.OnInputLeft += () => _pokemonStorageHandler.ChangeBox(-1);
        _inputStateHandler.OnInputRight += () => _pokemonStorageHandler.ChangeBox(1);
        return;
        void SwitchToExit()
        {
            if (_pokemonStorageHandler.movingPokemon) return;
            _inputStateHandler.ResetSpecificUi(InputStateName.PokemonStorageBoxChange,true);
            SetupPokemonStorageExit();
        }
        void SwitchToStorageBoxNavigation()
        {
            var storageBoxSelectables = new List<SelectableUI>();
            foreach (var icon in _pokemonStorageHandler.nonPartyIcons)
            { 
                var newSelectable = new SelectableUI(icon.gameObject,
                    ()=>_pokemonStorageHandler.SelectNonPartyPokemon(icon.GetComponent<PcStoragePokemon>())
                    , true);
                storageBoxSelectables.Add(newSelectable);
            }

            _inputStateHandler.ChangeInputState(new (InputStateName.PokemonStorageBoxNavigation,InputStateGroup.PokemonStorage
                ,stateDirection:InputDirection.Grid,selectableUis:storageBoxSelectables,
                selector:_pokemonStorageHandler.initialSelector, selecting:true,display: true,canManualExit:false,canExit:false));
            _inputStateHandler.ChangeSelectionIndex(0);
        }
    }
    private void PokemonStorageBoxChange()
    {
        var storageSelectables = new List<SelectableUI>();
        for (int i = 0; i < PokemonStorageHandler.NumBoxes; i++)
        {
            storageSelectables.Add(new(
                _pokemonStorageHandler.boxTopVisualImage.gameObject
                ,null, true));
        }
        _pokemonStorageHandler.initialSelector.transform.rotation = Quaternion.Euler(0, 0, 0);
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonStorageBoxChange,
            InputStateGroup.PokemonStorage, false, null,
            InputDirection.Horizontal,storageSelectables,
            _pokemonStorageHandler.initialSelector, selecting:true,display:true,canManualExit:false));
    }
}
