using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "Objectives/interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType interactionTypeForObjective;
   
   private DialogueHandler _dialogueHandler;
   private DialogueOptionsEventHandler _dialogueOptionsHandler;
   private OverworldState _overworldStateHandler;
   
   protected override void OnObjectiveLoaded()
   {
      _dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
      _dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>(); 
      _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
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
      if (interactionTypeForObjective != interaction.overworldInteraction) return;
      ClearObjective();
   }
   
   protected override void OnObjectiveCleared()
   {
      _dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
      _overworldStateHandler.ClearAndLoadNextObjective();
   }
}
