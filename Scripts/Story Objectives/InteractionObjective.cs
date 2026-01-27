using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType overworldInteractionForObjective;
   public bool hasSpecialLogic;
   
   protected Action onObjectiveComplete;
   
   protected override void OnObjectiveLoaded()
   {
      Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
      if(hasSpecialLogic)
         Dialogue_handler.Instance.OnOptionsDisplayed += InteractionResult;
      else Options_manager.Instance.OnInteractionOptionChosen += CheckInteraction;
   }
   

   protected virtual void InteractionResult(Overworld_interactable interactable) { }
   
   private void CheckInteraction(Overworld_interactable interactable, int optionChosen)
   {
      if (optionChosen>0)
      {
         Dialogue_handler.Instance.EndDialogue(); 
         return;
      }
      CheckEventMatch(interactable.overworldInteractionType);
   }
  
   private void CheckEventMatch(OverworldInteractionType interactionType)
   {
      if (overworldInteractionForObjective != interactionType) return;
      ClearObjective();
   }
   public override void ClearObjective()
   {
      Options_manager.Instance.OnInteractionOptionChosen -= CheckInteraction;
      OverworldState.Instance.ClearAndLoadNextObjective();
   }
}
