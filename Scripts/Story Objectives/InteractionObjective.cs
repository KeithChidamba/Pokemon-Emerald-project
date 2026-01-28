using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType overworldInteractionForObjective;
   protected Action onObjectiveComplete;
   
   protected override void OnObjectiveLoaded()
   {
      Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
      Options_manager.Instance.OnInteractionOptionChosen += CheckInteractionOption;
   }
   
   private void CheckInteractionOption(Overworld_interactable interactable, int optionChosen)
   {
      if (optionChosen>0)
      {
         Dialogue_handler.Instance.EndDialogue(); 
         return;
      }
      if (overworldInteractionForObjective != interactable.overworldInteractionType) return;
      ClearObjective();
   }

   public override void ClearObjective()
   {
      Options_manager.Instance.OnInteractionOptionChosen -= CheckInteractionOption;
      OverworldState.Instance.ClearAndLoadNextObjective();
   }
}
