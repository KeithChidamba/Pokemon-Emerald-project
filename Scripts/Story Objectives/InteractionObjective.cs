using System;
using UnityEngine;

[CreateAssetMenu(fileName = "interaction obj", menuName = "Objectives/interaction objective")]
public class InteractionObjective : StoryObjective
{
   public Interaction interactionForObjective;
   protected Action onObjectiveComplete;
   
   protected Dialogue_handler dialogueHandler;
   protected DialogueOptionsEventHandler dialogueOptionsHandler;
   protected OverworldState overworldStateHandler;
   
   protected override void OnObjectiveLoaded()
   {
      dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
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
      if (interactionForObjective.overworldInteraction != interaction.overworldInteraction) return;
      ClearObjective();
   }
   
   protected override void OnObjectiveCleared()
   {
      dialogueOptionsHandler.OnInteractionOptionChosen -= CheckInteractionOption;
      overworldStateHandler.ClearAndLoadNextObjective();
   }
}
