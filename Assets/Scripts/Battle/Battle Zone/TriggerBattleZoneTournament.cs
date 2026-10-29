using System;
[Serializable]

public class TriggerBattleZoneTournament : DynamicAdditionalInfo
{
    public override void HandleInternalLogic(ServiceContainer container)
    {
        var dialogueHandler = container.Resolve<DialogueHandler>();
        var battleZone = container.Resolve<BattleZoneHandler>();
        dialogueHandler.EndDialogue();
        battleZone.BeginTournament();
    }
}
