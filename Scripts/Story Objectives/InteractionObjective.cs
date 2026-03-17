using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "Objectives/interaction objective")]
public class InteractionObjective : StoryObjective
{
   public Interaction interactionForObjective;
   protected Action onObjectiveComplete;
   private Dialogue_handler _dialogueHandler;
   private Options_manager _dialogueOptionsHandler;
   
   protected override void OnObjectiveLoaded()
   {
      _dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
      _dialogueOptionsHandler = serviceContainer.Resolve<Options_manager>(); 
      _dialogueHandler.DisplayObjectiveText(objectiveHeading);
      _dialogueOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
   }
   
   private void CheckInteractionOption(Interaction interaction, int optionChosen)
   {
      if (optionChosen>0)
      {
         _dialogueHandler.EndDialogue(); 
         return;
      }
      if (interactionForObjective.overworldInteraction != interaction.overworldInteraction) return;
      ClearObjective();
   }
   
   protected override void OnObjectiveCleared()
   {
      var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
      _dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
      overworldStateHandler.ClearAndLoadNextObjective();
   }
}
