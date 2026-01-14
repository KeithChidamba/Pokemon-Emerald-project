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
            Options_manager.Instance.OnInteractionOptionChosen += CheckBattleInteraction;
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
        if (isTrainerBattle)
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
