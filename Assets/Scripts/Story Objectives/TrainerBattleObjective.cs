using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[CreateAssetMenu(fileName = "trainer battle obj", menuName = "Objectives/trainer battle objective")]
public class TrainerBattleObjective : StoryObjective
{
    public TrainerData trainer;
    public bool mustWin;
    private DialogueHandler _dialogueHandler;
    private BattleHandler _battleHandler;
    
    protected override void OnObjectiveLoaded()
    {
        _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
        _battleHandler = serviceContainer.Resolve<BattleHandler>(); 
        _dialogueHandler.DisplayObjectiveText(objectiveHeading);
        _battleHandler.OnTrainerBattleResult += CheckIfWin;
    }
    private void CheckIfWin(bool hasWon,List<TrainerData> trainersFromBattle)
    {
        if (mustWin)
        {
            if (!hasWon) return;
        }
        if (trainersFromBattle.Any(t=>t.trainerName == trainer.trainerName))
        {
            _battleHandler.OnTrainerBattleResult -= CheckIfWin;
            ClearObjective();
        }
    }
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}
