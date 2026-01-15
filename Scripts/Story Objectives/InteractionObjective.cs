using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType overworldInteractionForObjective;
   public bool requiresSuccess;
   public Item itemForObjective;
   private Action _onObjectiveComplete;

   public override void LoadObjective()
   {
      Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
      if (requiresSuccess)
      {
         Dialogue_handler.Instance.OnOptionsDisplayed += CheckForInteractionTrigger;
      }else
      {
         Options_manager.Instance.OnInteractionOptionChosen += CheckInteraction;
      }
   }

   private void CheckForInteractionTrigger(Overworld_interactable interactable)
   {
      if (overworldInteractionForObjective != interactable.overworldInteractionType) return;
      switch(overworldInteractionForObjective)
      {
         case OverworldInteractionType.PickBerry:
         case OverworldInteractionType.PlantBerry:
         case OverworldInteractionType.WaterBerryTree:
         {
            var berryTree = interactable.GetComponent<BerryTree>(); 

            Action<OverworldInteractionType,bool> checkEventSuccessBerry = 
               (interactionType,success)=> CheckEventSuccess(success,berryTree.treeData.berryItem.itemName);
             
            berryTree.OnInteractionComplete += checkEventSuccessBerry;
            _onObjectiveComplete += RemoveSubscription;
            
            break;
           
            void RemoveSubscription()
            {
               berryTree.OnInteractionComplete -= checkEventSuccessBerry;
               _onObjectiveComplete -= RemoveSubscription;
            } 
         }
      }
   }
   private void CheckInteraction(Overworld_interactable interactable, int optionChosen)
   {
      if (optionChosen>0)
      {
         Dialogue_handler.Instance.EndDialogue(); 
         return;
      }
      CheckEventMatch(interactable.overworldInteractionType);
   }
   private void CheckEventSuccess(bool successful,string nameCheck)
   {
      if (!successful) return;
      if(itemForObjective.itemName!=nameCheck) return;
      
      _onObjectiveComplete?.Invoke();
      ClearObjective();
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
