
using UnityEngine;
[CreateAssetMenu(fileName = "trainer battle obj", menuName = "trainer battle objective")]
public class TrainerBattleObjective : StoryObjective
{
    public TrainerData trainer;
    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
        Options_manager.Instance.OnInteractionOptionChosen += CheckBattleInteraction;
    }
    private void CheckBattleInteraction(Overworld_interactable interactable, int optionChosen)
    {
        if (interactable.interaction.overworldInteraction == OverworldInteractionType.Battle)
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
        Options_manager.Instance.OnInteractionOptionChosen -= CheckBattleInteraction;
       
        Battle_handler.Instance.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
