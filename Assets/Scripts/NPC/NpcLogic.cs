
using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcLogic : MonoBehaviour
{
    public NpcMovement movementHandler;
    public OverwolrdNpcData overworldData;
    [SerializeField]private bool playerDetected;
    [SerializeField]private bool constantScan;
    private bool _longDistanceDetection;
    private Action _runDialogueInteraction;
    
    public OverworldInteractable npcInteractable;
    
    /// <summary>
    /// Called when the npc is destroyed, is nullified after invoke
    /// </summary>
    public event Action OnObjectDestroy;
    
    private DialogueHandler _dialogueHandler;
    private PlayerMovementHandler _playerMovement;
    private BattleHandler _battleHandler;
    
    public void Inject(ServiceContainer container,OverwolrdNpcData data)
    {
        _dialogueHandler = container.Resolve<DialogueHandler>();
        _playerMovement = container.Resolve<PlayerMovementHandler>();
        _battleHandler = container.Resolve<BattleHandler>();
        
        overworldData = data;
        movementHandler.animationData = data.npcData.npcAnimationData;
        if (data.npcData.npcInteraction is null)
        {
            var newInteraction = ScriptableObject.CreateInstance<Interaction>();
            newInteraction.dialogueType = DialogType.Details;
            newInteraction.interactionMessage = data.npcData.npcInteractionMessage;
            npcInteractable.interaction = newInteraction;
        }
        else
        {
            npcInteractable.interaction = data.npcData.npcInteraction;
        }
        
        playerDetected = false;
        if (overworldData.autoDetectPlayer)
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
        _dialogueHandler.OnDialogueStarted += PauseForInteraction;
        _dialogueHandler.OnDialogueEnded += ResumeAfterInteraction;
        _playerMovement.OnNewTile += DetectPlayer;
        
        OnObjectDestroy += () =>
        {
            _dialogueHandler.OnDialogueStarted -= PauseForInteraction;
            _dialogueHandler.OnDialogueEnded -= ResumeAfterInteraction;
            _playerMovement.OnNewTile -= DetectPlayer;
        };
    }

    private void ResumeAfterInteraction(Interaction interaction)
    {
        if(!overworldData.resumeMovementAfterPause)return;
        if(interaction is null) return;
        if(interaction.GetID != npcInteractable.interaction.GetID) return;
        movementHandler.ResumeMovement();
    }
    
    private void PauseForInteraction(Interaction interaction)
    {
        if(interaction.GetID != npcInteractable.interaction.GetID) return;
        
        if (_longDistanceDetection)
        {
            movementHandler.OnMovementPaused -= _runDialogueInteraction;
        }
        
        if (!playerDetected)
        {
            //if the player is the one who interacted with npc
            movementHandler.FacePlayerDirection(_playerMovement);
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

        for (int i = 1; i <= overworldData.detectDistance; i++)
        {
            Vector3 checkPos = npcPos + (Vector3)(direction * i);

            if (checkPos == playerPos)
            {
                if (movementHandler.CountClearTiles(direction, i - 1) < i - 1) continue;
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
            _runDialogueInteraction = () =>
            {
                movementHandler.OnMovementEnded -= _runDialogueInteraction;
                _dialogueHandler.StartInteraction(npcInteractable.interaction);
            };
            movementHandler.OnMovementEnded += _runDialogueInteraction;
            movementHandler.MoveToSpecific(movementHandler.GetCurrentDirection(),distance-1);
        }
        else
        {
            movementHandler.StopMovement(false);
            _dialogueHandler.StartInteraction(npcInteractable.interaction);
        }
    }

    private void OnDestroy()
    {
        OnObjectDestroy?.Invoke();
        OnObjectDestroy = null;
    }
}
