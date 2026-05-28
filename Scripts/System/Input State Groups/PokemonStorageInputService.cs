using System;
using System.Collections.Generic;
using UnityEngine;


public class PokemonStorageInputService: IInputGroup
{
    private Game_ui_manager _gameUIHandler;
    private pokemon_storage _pokemonStorageHandler;
    private InputStateHandler _inputStateHandler;
    
    public PokemonStorageInputService(ServiceContainer container)
    {
        _inputStateHandler = container.Resolve<InputStateHandler>();
        _gameUIHandler = container.Resolve<Game_ui_manager>();
        _pokemonStorageHandler = container.Resolve<pokemon_storage>();
    }
    public void DetermineOperation()
    {
        _inputStateHandler.OnFullBoxNavigation += AllowTopRowExit;
        Action stateMethod = _inputStateHandler.currentState.stateName switch
        {
            InputStateName.PokemonStoragePartyNavigation=>LoadStoragePokemonData,
            InputStateName.PokemonStorageDepositSelection=>ShowStorageBoxCapacityData,
            InputStateName.PokemonStorageBoxNavigation => StorageFullBoxNavigation,
            _ => null
        };
        stateMethod?.Invoke();
    }

    private void AllowTopRowExit(int change,bool isVertical)
    {
        if (_inputStateHandler.boxCoordinates[0]==0 && change<0 && isVertical)
        {
            _inputStateHandler.OnSelectionIndexChanged += ExitTopRow;
        }
    }
    
    private void StorageFullBoxNavigation()
    {
        _inputStateHandler.currentNumBoxElements = pokemon_storage.BoxCapacity;
        _inputStateHandler.currentBoxCapacity = pokemon_storage.BoxCapacity;
        _inputStateHandler.numBoxColumns = pokemon_storage.BoxColumns;
        _inputStateHandler.numBoxRows = pokemon_storage.BoxCapacity / _inputStateHandler.numBoxColumns;
        _inputStateHandler.OnInputLeft += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Horizontal,-1);
        _inputStateHandler.OnInputRight += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Horizontal,1);
        _inputStateHandler.OnInputUp += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Vertical,-1);
        _inputStateHandler.OnInputDown += ()=> _inputStateHandler.MoveCoordinatesFullBox(InputDirection.Vertical,1);
        
        _inputStateHandler.currentState.canExit = false;
        _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.LoadPokemonData;
        _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.UpdateBoxPosition;
    }

    public void SetupPokemonStorageState()
    {
        var storageSelectables = new List<SelectableUI>{
            new(_pokemonStorageHandler.storageBoxExit.gameObject,_gameUIHandler.ClosePokemonStorage, true)
        };
        
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonStorageExit,
            InputStateGroup.PokemonStorage, true,_pokemonStorageHandler.storageUI,
            InputDirection.Vertical,storageSelectables,_pokemonStorageHandler.initialSelector, true,display:true,canManualExit:false
            ));
        _pokemonStorageHandler.initialSelector.transform.rotation = Quaternion.Euler(0, 180, 180);
        
        _inputStateHandler.OnInputDown += PokemonStorageBoxChange;
    }

    private void PokemonStorageBoxChange()
    {
        var storageSelectables = new List<SelectableUI>();
        for (int i = 0; i < pokemon_storage.NumBoxes; i++)
        {
            storageSelectables.Add(new(_pokemonStorageHandler.boxTopVisualImage.gameObject,null, true));
        }
        _pokemonStorageHandler.initialSelector.transform.rotation = Quaternion.Euler(0, 0, 0);
        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonStorageBoxChange,
            InputStateGroup.PokemonStorage, true,_pokemonStorageHandler.storageUI,
            InputDirection.Horizontal,storageSelectables,_pokemonStorageHandler.initialSelector, selecting:true,display:true,canManualExit:false));

        _inputStateHandler.OnInputUp += SwitchToExit;
        _inputStateHandler.OnInputDown += PokemonStorageBoxNavigation;
        _inputStateHandler.OnInputLeft += () => _pokemonStorageHandler.ChangeBox(-1);
        _inputStateHandler.OnInputRight += () => _pokemonStorageHandler.ChangeBox(1);
    }
    private void PokemonStorageBoxNavigation()
    {
        var storageBoxSelectables = new List<SelectableUI>();
        foreach (var icon in _pokemonStorageHandler.nonPartyIcons)
        { 
            var newSelectable = new SelectableUI(icon.gameObject,
                ()=>_pokemonStorageHandler.SelectNonPartyPokemon(icon.GetComponent<PC_pkm>())
                , true);
            storageBoxSelectables.Add(newSelectable);
        }

        _inputStateHandler.ChangeInputState(new (InputStateName.PokemonStorageBoxNavigation,InputStateGroup.PokemonStorage
            ,stateDirection:InputDirection.Grid,selectableUis:storageBoxSelectables,
            selector:_pokemonStorageHandler.initialSelector, selecting:true,display: true,canManualExit:false,canExit:false));
        _inputStateHandler.ChangeSelectionIndex(0);
    }
    private void SwitchToExit()
    {
        if (_pokemonStorageHandler.movingPokemon) return;
        _inputStateHandler.RemoveTopInputLayer(false);
        SetupPokemonStorageState();
    }
    private void ExitTopRow(int index)
    {
        _inputStateHandler.RemoveTopInputLayer(false);
        _pokemonStorageHandler.ClearPokemonData();
        PokemonStorageBoxChange();
    }
    void LoadStoragePokemonData()
    {
        _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.LoadPokemonData;
    }
    void ShowStorageBoxCapacityData()
    {
        _inputStateHandler.OnSelectionIndexChanged += _pokemonStorageHandler.DisplayBoxCapacity;
    }
}
