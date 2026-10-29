using System;
[Serializable]
public class BattleTriggerInfo :  DynamicAdditionalInfo
{
   public TrainerData trainerData;  
   public override void HandleInternalLogic(ServiceContainer container)
   {
      var dialogueHandler = container.Resolve<DialogueHandler>();
      var battleHandler = container.Resolve<BattleHandler>();
      dialogueHandler.EndDialogue(); 
      battleHandler.StartBattle(trainerData);
   }
}
