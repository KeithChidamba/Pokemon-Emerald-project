
using UnityEngine;
[CreateAssetMenu(fileName = "storage ui obj", menuName = "Objectives/storage ui objective")]
public class PokemonStorageObjective : StoryObjective
{
    private enum StorageObjectiveType
    {
        WithdrawPokemonFromPC,DepositPokemonIntoPC
    }
    
    [SerializeField] private StorageObjectiveType storageObjectiveType;
    public bool spawnPokemonInPc;
    public PokemonGiftInteractoinInfo pokemonForObjective;
   
    private PokemonStorageHandler _pokemonStorageHandler;
    private PokemonOperations _pokemonOperationsHandler;
    private GameUiHandler _gameUiHandler;
    private GameLoadingHandler _gameLoadingHandler;
    
    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        
        _pokemonStorageHandler = serviceContainer.Resolve<PokemonStorageHandler>();
        _pokemonOperationsHandler = serviceContainer.Resolve<PokemonOperations>();
        _gameUiHandler = serviceContainer.Resolve<GameUiHandler>();
        _gameLoadingHandler = serviceContainer.Resolve<GameLoadingHandler>();
        
        if (spawnPokemonInPc)
        {
            _gameUiHandler.SetMenuAccessibility(false); 
            _pokemonOperationsHandler.CreateSpecificPokemon(
            newPokemon =>
            {
                PokemonOperations.ResolvePokemonNickName(newPokemon);
                newPokemon.hasTrainer = true;
                newPokemon.pokeballName = NameDB.GetItem(ItemName.Pokeball); 
                newPokemon.ChangeFriendshipLevel(120);
                newPokemon.captureInformation.levelCaptured = newPokemon.currentLevel;
                newPokemon.captureInformation.areaName = AreaManager.GetAreaName(_gameLoadingHandler.playerData.location);
                _pokemonStorageHandler.AddPokemonToStorage(newPokemon);
            }
            ,pokemonForObjective.giftPokemon
            ,pokemonForObjective.pokemonLevel
            ,pokemonForObjective.evolutionStageNumber);
        }
        
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
        if (pokemon.pokemonName == pokemonForObjective.giftPokemon.pokemonName)
        {
            if(storageObjectiveType == StorageObjectiveType.WithdrawPokemonFromPC)
                _pokemonStorageHandler.OnPokemonWithdraw -= CheckForStorageObjectiveClear;
            else _pokemonStorageHandler.OnPokemonDeposit -= CheckForStorageObjectiveClear;
            ClearObjective();
        }
    }
    
    protected override void OnObjectiveCleared()
    {
        if (spawnPokemonInPc)
        {
            _gameUiHandler.SetMenuAccessibility(true);
        }
        
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}

