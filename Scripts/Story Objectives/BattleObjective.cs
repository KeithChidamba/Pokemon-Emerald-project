using UnityEngine;
[CreateAssetMenu(fileName = "battle obj", menuName = "battle objective")]
public class BattleObjective : StoryObjective
{
    public enum BattleObjectiveOutline
    {
        BeatTrainer,BeatWildPokemon,CatchWildPokemon
    }
    public BattleObjectiveOutline objectiveOutline;
    public TrainerData trainer;
    public BattleEncounterSource encounterSourceForObjective;
    
    public override void LoadObjective()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
        if (objectiveOutline == BattleObjectiveOutline.BeatTrainer)
        {
            Options_manager.Instance.OnInteractionOptionChosen += CheckBattleInteraction;
        }
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
        if (!isCaught)
        {
            return;
        }
        PokemonOperations.Instance.OnPokeballUsed -= CheckIfPokemonCaught;
        ClearObjective();
    }
    private void CheckEncounter(BattleEncounterSource battleEncounterSource)
    {
        if (battleEncounterSource == encounterSourceForObjective)
        {
            Battle_handler.Instance.OnBattleResult += CheckIfWin;
        }
    }
    private void CheckBattleInteraction(Overworld_interactable interactable, int optionChosen)
    {
        if (interactable.overworldInteractionType == OverworldInteractionType.Battle)
        {
            if (trainer.TrainerName == interactable.interaction.additionalInfo[0])
            {
                Battle_handler.Instance.OnBattleResult += CheckIfWin;
            }
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        if (!hasWon) return;
        if (objectiveOutline == BattleObjectiveOutline.BeatTrainer)
        {
            Options_manager.Instance.OnInteractionOptionChosen -= CheckBattleInteraction;
        }
        else
        {
            Encounter_handler.Instance.OnEncounterTriggered -= CheckEncounter;
        }
        Battle_handler.Instance.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
