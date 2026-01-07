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
            Debug.Log("started interaction check");
            Options_manager.Instance.OnInteractionTriggered += CheckBattleInteraction;
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
        Debug.Log("checked interaction");
        if (interactable.overworldInteractionType == OverworldInteractionType.Battle)
        {
            if (trainer.TrainerName == interactable.interaction.additionalInfo[0])
            {
                Debug.Log("checked trainer name");
                Battle_handler.Instance.OnBattleResult += CheckIfWin;
            }
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        Debug.Log("checked win");
        if (!hasWon) return;
        if (isTrainerBattle)
        {
            Options_manager.Instance.OnInteractionTriggered -= CheckBattleInteraction;
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
