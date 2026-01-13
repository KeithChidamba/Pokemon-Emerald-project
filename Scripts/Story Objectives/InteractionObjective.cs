using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using System;

[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType overworldInteractionForObjective;
   public bool requiresSuccess;
   public Item itemForObjective;
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

      if (requiresSuccess )
      {
         switch(overworldInteractionForObjective)
         {
            case OverworldInteractionType.PickBerry:
            case OverworldInteractionType.PlantBerry:
            case OverworldInteractionType.WaterBerryTree:
            {
               var berryTree = interactable.GetComponent<BerryTree>();
               berryTree.OnInteractionComplete += (type,success)=> CheckEventMatch(type,success,berryTree.treeData.berryItem.itemName);
               break;
            }
         }
      }
      else
      {
         CheckEventMatch(interactable.overworldInteractionType);
      }
   }
   
   private void CheckEventMatch(OverworldInteractionType interactionType,bool successfull,string nameCheck)
   {
      if (overworldInteractionForObjective != interactionType) return;
      if (!successfull) return;
      if(itemForObjective.itemName!=nameCheck) return;
      ClearObjective();
   }
   private void CheckEventMatch(OverworldInteractionType interactionType)
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
