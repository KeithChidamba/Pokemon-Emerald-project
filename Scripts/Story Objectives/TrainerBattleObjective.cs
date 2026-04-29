using UnityEngine;
[CreateAssetMenu(fileName = "trainer battle obj", menuName = "Objectives/trainer battle objective")]
public class TrainerBattleObjective : StoryObjective
{
    public TrainerData trainer;
    private Dialogue_handler _dialogueHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private Battle_handler _battleHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        _dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>(); 
        _battleHandler = serviceContainer.Resolve<Battle_handler>(); 
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _dialogueOptionsHandler.OnInteractionOptionChosen += CheckBattleInteraction;
    }
    private void CheckBattleInteraction(Interaction interaction, int optionChosen)
    {
        if (interaction.overworldInteraction == OverworldInteractionType.Battle)
        {
            if (trainer.TrainerName == interaction.additionalInfo[0])
            {
                _battleHandler.OnBattleResult += CheckIfWin;
            }
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        if (!hasWon) return;
        _dialogueOptionsHandler.OnInteractionOptionChosen -= CheckBattleInteraction;
       
        _battleHandler.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
