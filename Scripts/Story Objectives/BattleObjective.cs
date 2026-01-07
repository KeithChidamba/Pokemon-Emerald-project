using UnityEngine;
[CreateAssetMenu(fileName = "battle obj", menuName = "battle objective")]
public class BattleObjective : StoryObjective
{
    public bool isTrainerBattle;
    public TrainerData trainer;
    public BattleSource battleSourceForObjective;

    public override void LoadObjective()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
        if (isTrainerBattle)
        {
            Options_manager.Instance.OnInteractionTriggered += CheckInteraction;
        }
        else
        {
            Encounter_handler.Instance.OnEncounterTriggered += CheckEncounter;
        }
    }

    private void CheckEncounter(BattleSource battleSource)
    {
        if (battleSource == battleSourceForObjective)
        {
            Battle_handler.Instance.OnBattleResult += CheckIfWin;
        }
    }
    private void CheckInteraction(Overworld_interactable interactable, int optionChosen)
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
        if (isTrainerBattle)
        {
            Options_manager.Instance.OnInteractionTriggered -= CheckInteraction;
        }
        else
        {
            Encounter_handler.Instance.OnEncounterTriggered -= CheckEncounter;
        }
        ClearObjective();
    }
    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
