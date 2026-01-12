
using UnityEngine;
[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType overworldInteractionForObjective;
   public bool requiresSuccess;
   
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

      if (requiresSuccess)
      {
         switch(overworldInteractionForObjective)
         {
            case OverworldInteractionType.PickBerry:
            case OverworldInteractionType.PlantBerry:
            case OverworldInteractionType.WaterBerryTree:
            {
               var berryTree = interactable.GetComponent<BerryTree>();
               berryTree.OnInteractionSuccessful += CheckRequirement;
               break;
            }
         }
      }
      else
      {
         CheckRequirement(interactable.overworldInteractionType);
      }
   }
   
   private void CheckRequirement(OverworldInteractionType interactionType)
   {
      if (overworldInteractionForObjective != interactionType) return;
      ClearObjective();
   }
   public override void ClearObjective()
   {
      Options_manager.Instance.OnInteractionTriggered -= CheckInteraction;
      OverworldState.Instance.ClearAndLoadNextObjective();
   }
}
