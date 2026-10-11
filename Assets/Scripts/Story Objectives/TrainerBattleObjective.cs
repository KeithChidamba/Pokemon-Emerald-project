using UnityEngine;
[CreateAssetMenu(fileName = "trainer battle obj", menuName = "Objectives/trainer battle objective")]
public class TrainerBattleObjective : StoryObjective
{
    public TrainerData trainer;
    private DialogueHandler _dialogueHandler;
    private InteractionOptionsEventHandler interactionOptionsHandler;
    private BattleHandler _battleHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        interactionOptionsHandler = serviceContainer.Resolve<InteractionOptionsEventHandler>(); 
        _battleHandler = serviceContainer.Resolve<BattleHandler>(); 
        _dialogueHandler.DisplayObjectiveText($"Defeat {trainer.TrainerName}");
        interactionOptionsHandler.OnEventInteraction += CheckBattleInteraction;
    }
    private void CheckBattleInteraction(Interaction interaction)
    {
        if (interaction.overworldInteraction == OverworldInteractionType.Battle)
        {
            var trainerInteraction = interaction.GetModule<TrainerData>();
            if (trainer.TrainerName == trainerInteraction.TrainerName)
            {
                _battleHandler.OnBattleResult += CheckIfWin;
            }
        }
    }

    private void CheckIfWin(bool hasWon)
    {
        if (!hasWon) return;
        interactionOptionsHandler.OnEventInteraction -= CheckBattleInteraction;
       
        _battleHandler.OnBattleResult -= CheckIfWin;
        ClearObjective();
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
