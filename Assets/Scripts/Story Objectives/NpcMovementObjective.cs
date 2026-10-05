
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "npc obj", menuName = "Objectives/Npc Movement objective")]
public class NpcMovementObjective : NpcStoryObjective
{
   public List<NpcMovementDirection> movementDirections = new ();
   public int npcSpeed = 2;
   private PlayerMovementHandler _playerMovementHandler;
   public float pauseDelay = 1f;
   public bool waitForPlayer;
   
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
      int movementIndex = 1;
      if (waitForPlayer)
      {
         _playerMovementHandler.OnNewTile += CheckPlayerPosition;
      }
      _playerMovementHandler.canRun = false;
      npcLogic.movementHandler.SetPauseDelay(pauseDelay);
      npcLogic.movementHandler.OnMovementEnded += StartNextMovement;
      npcLogic.movementHandler.MoveToSpecific(movementDirections[0].direction, movementDirections[0].numTilesToTravel,npcSpeed);
      return;
      void StartNextMovement()
      {
         if (movementIndex >= movementDirections.Count)
         {
            npcLogic.movementHandler.OnMovementEnded -= StartNextMovement;
            _playerMovementHandler.canRun = true;
            if (!waitForPlayer)
            {
               ClearAndHandleRemoval();
            }
            return;
         }
         npcLogic.movementHandler.MoveToSpecific(movementDirections[movementIndex].direction,
            movementDirections[movementIndex].numTilesToTravel,npcSpeed);
         movementIndex++;
      }
      void CheckPlayerPosition()
      {
         if (movementIndex < movementDirections.Count) return;//Npc still moving
         
         var playerPos = _playerMovementHandler.GetPlayerPosition();
         //Check that the player is close enough to npc
         var npcFinalPos = npcLogic.movementHandler.transform.position;
         if(IsOneUnitAway2D(playerPos,npcFinalPos))
         {
            _playerMovementHandler.OnNewTile -= CheckPlayerPosition;
            ClearAndHandleRemoval();
         }
      }
   }
   bool IsOneUnitAway2D(Vector3 a, Vector3 b)
   {
      float dx = Mathf.Abs(a.x - b.x);
      float dy = Mathf.Abs(a.y - b.y);

      return (Mathf.Approximately(dx, 1f) && Mathf.Approximately(dy, 0f)) ||
             (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 1f));
   }
}
