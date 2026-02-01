using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public Interaction interactionForObjective;
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
      if (interactionForObjective.overworldInteraction != interactable.interaction.overworldInteraction) return;
      ClearObjective();
   }
   
   protected override void OnObjectiveCleared()
   {
      Options_manager.Instance.OnInteractionOptionChosen -= CheckInteractionOption;
      OverworldState.Instance.ClearAndLoadNextObjective();
   }
}
