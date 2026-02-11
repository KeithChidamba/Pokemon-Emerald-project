using UnityEngine;
[CreateAssetMenu(fileName = "trainer battle obj", menuName = "Objectives/trainer battle objective")]
public class TrainerBattleObjective : StoryObjective
{
    public TrainerData trainer;
    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
        Options_manager.Instance.OnInteractionOptionChosen += CheckBattleInteraction;
    }
    private void CheckBattleInteraction(Interaction interaction, int optionChosen)
    {
        if (interaction.overworldInteraction == OverworldInteractionType.Battle)
        {
            if (trainer.TrainerName == interaction.additionalInfo[0])
            {
                Battle_handler.Instance.OnBattleResult += CheckIfWin;
            }
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        if (!hasWon) return;
        Options_manager.Instance.OnInteractionOptionChosen -= CheckBattleInteraction;
       
        Battle_handler.Instance.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
