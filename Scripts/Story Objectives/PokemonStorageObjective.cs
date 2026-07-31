using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "storage ui obj", menuName = "Objectives/storage ui objective")]
public class PokemonStorageObjective : UiActionObjective
{
    private enum StorageObjectiveType
    {
        WithdrawPokemonFromPC,DepositPokemonIntoPC
    }
    [SerializeField] private StorageObjectiveType storageObjectiveType;
    public Pokemon pokemonForObjective;
    private PokemonStorageHandler _pokemonStorageHandler;
    
    protected override void LogicForObjectiveLoad()
    {
        _pokemonStorageHandler = serviceContainer.Resolve<PokemonStorageHandler>(); 
        switch(storageObjectiveType)
        {
            case StorageObjectiveType.WithdrawPokemonFromPC: WithdrawObjective(); break;
            case StorageObjectiveType.DepositPokemonIntoPC: DepositObjective(); break;
        }
    }
    private void WithdrawObjective()
    {
        _pokemonStorageHandler.OnPokemonWithdraw += CheckForStorageObjectiveClear;
    }
    private void DepositObjective()
    {
        _pokemonStorageHandler.OnPokemonDeposit += CheckForStorageObjectiveClear;
    }
    private void CheckForStorageObjectiveClear(Pokemon pokemon)
    {
        if (pokemon.pokemonName == pokemonForObjective.pokemonName)
        {
            if(storageObjectiveType == StorageObjectiveType.WithdrawPokemonFromPC)
                _pokemonStorageHandler.OnPokemonWithdraw -= CheckForStorageObjectiveClear;
            else _pokemonStorageHandler.OnPokemonDeposit -= CheckForStorageObjectiveClear;
            ClearObjective();
        }
    }
}

