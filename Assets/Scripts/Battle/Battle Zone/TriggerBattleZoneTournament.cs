using System;
[Serializable]

public class TriggerBattleZoneTournament : DynamicAdditionalInfo
{
    public override void HandleInternalLogic(ServiceContainer container)
    {
        var dialogueHandler = container.Resolve<DialogueHandler>();
        var battleZone = container.Resolve<BattleZoneHandler>();
        dialogueHandler.EndDialogue();
        
        dialogueHandler.DisplayCustomOptions($"What difficulty mode would you like?",
            new[] { "Easy", "Hard", "Crazy" }, new Action[]
            {
                ()=>battleZone.SetDifficultyAndStart(BattleCompetitionDifficulty.Easy),
                ()=>battleZone.SetDifficultyAndStart(BattleCompetitionDifficulty.Hard),
                ()=>battleZone.SetDifficultyAndStart(BattleCompetitionDifficulty.Crazy)
            });
    }
}
