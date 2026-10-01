using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "npc obj", menuName = "Objectives/Npc Movement objective")]
public class NpcMovementObjective : NpcStoryObjective
{
   public List<NpcMovementDirection> movementDirections = new ();
   public int npcSpeed = 2;
   private PlayerMovementHandler _playerMovementHandler;
   
   protected override void OnObjectiveLoaded()
   {
      dialogueHandler = serviceContainer.Resolve<DialogueHandler>(); 
      areaManager = serviceContainer.Resolve<AreaManager>();
      _playerMovementHandler = serviceContainer.Resolve<PlayerMovementHandler>();
      
      dialogueHandler.DisplayObjectiveText(objectiveHeading);
      SetupNpc();
      HandleMovement();
   }
   private void HandleMovement()
   {
      _playerMovementHandler.canRun = false;
      int movementIndex = 1;
      npcLogic.movementHandler.OnMovementEnded += StartNextMovement;
      npcLogic.movementHandler.MoveToSpecific(movementDirections[0].direction, movementDirections[0].numTilesToTravel,npcSpeed);
      return;
      void StartNextMovement()
      {
         if (movementIndex >= movementDirections.Count)
         {
            npcLogic.movementHandler.OnMovementEnded -= StartNextMovement;
            ClearAndHandleRemoval();
            _playerMovementHandler.canRun = true;
            return;
         }
         npcLogic.movementHandler.MoveToSpecific(movementDirections[movementIndex].direction,
            movementDirections[movementIndex].numTilesToTravel,npcSpeed);
         movementIndex++;
      }
   }
}
