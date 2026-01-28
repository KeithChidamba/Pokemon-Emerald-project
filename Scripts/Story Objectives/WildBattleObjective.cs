using UnityEngine;
[CreateAssetMenu(fileName = "wild battle obj", menuName = "wild battle objective")]
public class WildBattleObjective : StoryObjective
{
    public enum BattleObjectiveOutline
    {
        BeatWildPokemon,CatchWildPokemon
    }
    public BattleObjectiveOutline objectiveOutline;
    public BattleEncounterSource encounterSourceForObjective;
    public Pokemon pokemonForObjective;
    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
        if (objectiveOutline == BattleObjectiveOutline.BeatWildPokemon)
        {
            Encounter_handler.Instance.OnEncounterTriggered += CheckEncounter;
        }
        if (objectiveOutline == BattleObjectiveOutline.CatchWildPokemon)
        {
            PokemonOperations.Instance.OnPokeballUsed += CheckIfPokemonCaught;
        }
    }
    private void CheckIfPokemonCaught(Pokemon pokemon,bool isCaught)
    {
        if (!isCaught || pokemonForObjective.pokemonName!=pokemon.pokemonName)
        {
            return;
        }
        PokemonOperations.Instance.OnPokeballUsed -= CheckIfPokemonCaught;
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
            Battle_handler.Instance.OnBattleResult += CheckIfWin;
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        if (!hasWon) return;
        Encounter_handler.Instance.OnEncounterTriggered -= CheckEncounter;
        Battle_handler.Instance.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
