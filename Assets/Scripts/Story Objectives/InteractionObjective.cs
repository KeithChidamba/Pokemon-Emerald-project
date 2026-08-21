using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "Objectives/interaction objective")]
public class InteractionObjective : StoryObjective
{
   public OverworldInteractionType interactionTypeForObjective;
   protected Action onObjectiveComplete;
   
   protected DialogueHandler dialogueHandler;
   protected DialogueOptionsEventHandler dialogueOptionsHandler;
   protected OverworldState overworldStateHandler;
   
   protected override void OnObjectiveLoaded()
   {
      dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
      dialogueOptionsHandler = serviceContainer.Resolve<DialogueOptionsEventHandler>(); 
      overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
      dialogueHandler.DisplayObjectiveText(objectiveHeading);
      dialogueOptionsHandler.OnInteractionOptionChosen += CheckInteractionOption;
   }
   
   private void CheckInteractionOption(Interaction interaction, int optionChosen)
   {
      if (optionChosen>0)
      {
         dialogueHandler.EndDialogue(); 
         return;
      }
      if (interactionTypeForObjective != interaction.overworldInteraction) return;
      ClearObjective();
   }
   
   protected override void OnObjectiveCleared()
   {
      dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
      overworldStateHandler.ClearAndLoadNextObjective();
   }
}
