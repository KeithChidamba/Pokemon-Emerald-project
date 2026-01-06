
using UnityEngine;
[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public InteractionType interactionForObjective;
   public override void LoadObjective()
   {
      Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
      Options_manager.Instance.OnInteractionTriggered += CheckInteraction;
   }

   private void CheckInteraction(Overworld_interactable interactable, int optionChosen)
   {
      if (optionChosen>0)
      {
         Dialogue_handler.Instance.EndDialogue(); 
         return;
      }

      if (interactionForObjective == interactable.interactionType)
      {
         ClearObjective();
      }
      
   }
   public override void ClearObjective()
   {
      OverworldState.Instance.ClearAndLoadNextObjective();
   }
}
