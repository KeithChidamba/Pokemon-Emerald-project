using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "storage ui obj", menuName = "storage ui objective")]
public class PokemonStorageObjective : UiActionObjective
{
    private enum StorageObjectiveType
    {
        WithdrawPokemonFromPC,DepositPokemonIntoPC
    }
    [SerializeField] private StorageObjectiveType storageObjectiveType;
    public Pokemon pokemonForObjective;
    protected override void OnObjectiveLoaded()
    {
        switch(storageObjectiveType)
        {
            case StorageObjectiveType.WithdrawPokemonFromPC: WithdrawObjective(); break;
            case StorageObjectiveType.DepositPokemonIntoPC: DepositObjective(); break;
        }
    }
    private void WithdrawObjective()
    {
        pokemon_storage.Instance.OnPokemonWithdraw += CheckForStorageObjectiveClear;
    }
    private void DepositObjective()
    {
        pokemon_storage.Instance.OnPokemonDeposit += CheckForStorageObjectiveClear;
    }
    private void CheckForStorageObjectiveClear(Pokemon pokemon)
    {
        if (pokemon.pokemonName == pokemonForObjective.pokemonName)
        {
            if(storageObjectiveType == StorageObjectiveType.WithdrawPokemonFromPC)
                pokemon_storage.Instance.OnPokemonWithdraw -= CheckForStorageObjectiveClear;
            else pokemon_storage.Instance.OnPokemonDeposit -= CheckForStorageObjectiveClear;
            ClearObjective();
        }
    }
}

