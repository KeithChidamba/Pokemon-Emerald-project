
using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcLogic : MonoBehaviour,IInjectable
{
    public NpcMovement movementHandler; 
    public bool autoDetectPlayer;
    [SerializeField]private float detectDistance = 1f;
    public Overworld_interactable npcInteractable;
    [SerializeField]private bool constantScan;
    [SerializeField]private bool playerDetected;
    private Action _runDialogueInteraction;
    private bool _longDistanceDetection;

    private Dialogue_handler _dialogueHandler;
    private Player_movement _playerMovement;
    private DialogueOptionsEventHandler _dialogueOptionsHandler;
    public void Inject(ServiceContainer container)
    {
        _dialogueOptionsHandler = container.Resolve<DialogueOptionsEventHandler>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _playerMovement = container.Resolve<Player_movement>();
        OnInject();
    }

    private void OnInject()
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
        _dialogueOptionsHandler.OnInteractionOptionChosen += PauseForInteraction;
        _playerMovement.OnNewTile += DetectPlayer;
    }

    private void PauseForInteraction(Interaction interaction,int optionChosen)
    {
        if(interaction.overworldInteraction!=OverworldInteractionType.Battle)return;
        if(interaction!=npcInteractable.interaction)return;
        
        if (_longDistanceDetection)
        {
            movementHandler.OnMovementPaused -= _runDialogueInteraction;
        }

        if (!playerDetected)
        {
            //if the player is the one who interacted with npc
            movementHandler.FacePlayerDirection();
        }
        
        playerDetected = false;
        _longDistanceDetection = false;
    }

    private void DetectPlayer()
    {
        if (!constantScan || playerDetected) return;
        
        Vector2 direction = movementHandler.GetDirectionAsVector();
        Vector3 npcPos = transform.position;

        Vector3 playerPos = _playerMovement.GetPlayerPosition();

        playerDetected = false;

        for (int i = 1; i <= detectDistance; i++)
        {
            Vector3 checkPos = npcPos + (Vector3)(direction * i);

            if (checkPos == playerPos)
            {
                playerDetected = true;
                break;
            }
        }

        if (!playerDetected) return;
       
        constantScan = false;
        
        _playerMovement.FaceOppositeDirection(movementHandler.GetCurrentDirection());
        
        //positions are always locked to whole numbers
        var distance = (int)Vector3.Distance(transform.position, _playerMovement.GetPlayerPosition());

        if (distance>1)
        {
            _longDistanceDetection = true;
            _runDialogueInteraction = () => _dialogueHandler.StartInteraction(npcInteractable.interaction);
            movementHandler.OnMovementEnded += _runDialogueInteraction;
            StartCoroutine(movementHandler.MoveToSpecific(movementHandler.GetCurrentDirection(),distance-1));
        }
        else
        {
            movementHandler.StopMovement(false);
            _dialogueHandler.StartInteraction(npcInteractable.interaction);
        }
    }
}
