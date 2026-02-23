
using System;

using UnityEngine;

public class NpcLogic : MonoBehaviour
{
    public NpcMovement movementHandler;
    public bool autoDetectPlayer;
    [SerializeField]private float detectDistance = 1f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField]private Interaction npcInteraction;
    public Transform rayCastPoint;
    [SerializeField]private bool constantScan;
    [SerializeField]private bool playerDetected;
    private Action _runDialogueInteraction;
    private bool _longDistanceDetection;
    private void Start()
    {
        playerDetected = false;
        if (autoDetectPlayer)
        {
            if (movementHandler.animationData.isIdle)
            {
                constantScan = true;
            }
            else
            {
                movementHandler.OnMovementPaused += ()=> constantScan = true;
                movementHandler.OnMovementStarted += ()=> constantScan = false;
            }
        }
        Options_manager.Instance.OnInteractionOptionChosen += PauseForInteraction;
    }

    private void Update()
    {
        if (!constantScan || playerDetected) return;
        
        DetectPlayer(movementHandler.GetDirectionAsVector());
    }

    private void PauseForInteraction(Interaction interaction,int optionChosen)
    {
        if(interaction.overworldInteraction!=OverworldInteractionType.Battle)return;
        if(interaction!=npcInteraction)return;
        
        if (_longDistanceDetection)
        {
            movementHandler.OnMovementPaused -= _runDialogueInteraction;
        }

        if (!playerDetected) movementHandler.FacePlayerDirection();
        
        playerDetected = false;
        _longDistanceDetection = false;
    }

    private void DetectPlayer(Vector2 directionVector)
    {
        var hit = Physics2D.Raycast(
            rayCastPoint.position,
            directionVector,
            detectDistance,
            playerLayer
        );       

        if (!hit) return;
        
        playerDetected = true;
        constantScan = false;
        
        Player_movement.Instance.FaceOppositeDirection(movementHandler.GetCurrentDirection());
        
        //positions are always locked to whole numbers
        var distance = (int)Vector3.Distance(transform.position, Player_movement.Instance.GetPlayerPosition());

        if (distance>1)
        {
            _longDistanceDetection = true;
            _runDialogueInteraction = () => Dialogue_handler.Instance.StartInteraction(npcInteraction);
            movementHandler.OnMovementEnded += _runDialogueInteraction;
            StartCoroutine(movementHandler.MoveToSpecific(movementHandler.GetCurrentDirection(),distance-1));
        }
        else
        {
            movementHandler.StopMovement(false);
            Dialogue_handler.Instance.StartInteraction(npcInteraction);
        }
    }
}
