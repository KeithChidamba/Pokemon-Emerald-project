using System;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class DialogueDisplayInfo : DynamicAdditionalInfo
{
   public List<string> dialogue = new();
   
   public override void HandleInternalLogic(ServiceContainer container)
   {
      var dialogueHandler = container.Resolve<DialogueHandler>();
      StringBuilder sb = new StringBuilder();
      foreach (var message in dialogue)
      {
         sb.Append(message);
      }
      dialogueHandler.DisplayFromBuilder(sb);
   }
}
