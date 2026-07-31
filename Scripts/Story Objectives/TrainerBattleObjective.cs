using UnityEngine;
[CreateAssetMenu(fileName = "trainer battle obj", menuName = "Objectives/trainer battle objective")]
public class TrainerBattleObjective : StoryObjective
{
    public TrainerData trainer;
    private DialogueHandler _dialogueHandler;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    private BattleHandler _battleHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>(); 
        _battleHandler = serviceContainer.Resolve<BattleHandler>(); 
        _dialogueHandler.DisplayObjectiveText($"Defeat {trainer.TrainerName}");
        _dialogueOptionsHandler.OnInteractionOptionChosen += CheckBattleInteraction;
    }
    private void CheckBattleInteraction(Interaction interaction, int optionChosen)
    {
        if (interaction.overworldInteraction == OverworldInteractionType.Battle)
        {
            var trainerInteraction = interaction.GetModule<TrainerBattleInteractionInfo>();
            if (trainer.TrainerName == trainerInteraction.data.TrainerName)
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
