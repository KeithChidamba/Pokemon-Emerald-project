using UnityEngine;
[CreateAssetMenu(fileName = "wild battle obj", menuName = "Objectives/wild battle objective")]
public class WildBattleObjective : StoryObjective
{
    public enum BattleObjectiveOutline
    {
        BeatWildPokemon,CatchWildPokemon
    }
    public BattleObjectiveOutline objectiveOutline;
    public BattleEncounterSource encounterSourceForObjective;
    public Pokemon pokemonForObjective;
    private Battle_handler _battleHandler;
    private Encounter_handler  _encounterHandler;
    private PokemonOperations _pokemonOperationsHandler;
    
    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        _encounterHandler = serviceContainer.Resolve<Encounter_handler>(); 
        _battleHandler = serviceContainer.Resolve<Battle_handler>(); 
        _pokemonOperationsHandler = serviceContainer.Resolve<PokemonOperations>(); 
        
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        
        if (objectiveOutline == BattleObjectiveOutline.BeatWildPokemon)
        {
            _encounterHandler.OnEncounterTriggered += CheckEncounter;
        }
        if (objectiveOutline == BattleObjectiveOutline.CatchWildPokemon)
        {
            _pokemonOperationsHandler.OnPokeballUsed += CheckIfPokemonCaught;
        }
    }
    private void CheckIfPokemonCaught(Pokemon pokemon,bool isCaught)
    {
        if (!isCaught || pokemonForObjective.pokemonName!=pokemon.pokemonName)
        {
            return;
        }
        _pokemonOperationsHandler.OnPokeballUsed -= CheckIfPokemonCaught;
        ClearObjective();
    }
    private void CheckEncounter(Pokemon wildPokemon,BattleEncounterSource battleEncounterSource)
    {
        if (pokemonForObjective.pokemonName!=wildPokemon.pokemonName)
        {
            return;
        }
        if (battleEncounterSource == encounterSourceForObjective)
        {
            _battleHandler.OnBattleResult += CheckIfWin;
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        if (!hasWon) return;
        _encounterHandler.OnEncounterTriggered -= CheckEncounter;
        _battleHandler.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
