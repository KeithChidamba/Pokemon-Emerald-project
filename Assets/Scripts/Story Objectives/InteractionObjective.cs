using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "Objectives/interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType interactionTypeForObjective;
   
   private DialogueHandler _dialogueHandler;
   private InteractionOptionsEventHandler interactionOptionsHandler;
   private OverworldState _overworldStateHandler;
   
   protected override void OnObjectiveLoaded()
   {
      _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
      interactionOptionsHandler = serviceContainer.Resolve<InteractionOptionsEventHandler>(); 
      _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
      _dialogueHandler.DisplayObjectiveText(objectiveHeading);
      interactionOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
   }
   
   private void CheckInteractionOption(Interaction interaction, int optionChosen)
   {
      if (optionChosen>0)
      {
         _dialogueHandler.EndDialogue(); 
         return;
      }
      if (interactionTypeForObjective != interaction.overworldInteraction) return;
      ClearObjective();
   }
   
   protected override void OnObjectiveCleared()
   {
      interactionOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
      _overworldStateHandler.ClearAndLoadNextObjective();
   }
}
