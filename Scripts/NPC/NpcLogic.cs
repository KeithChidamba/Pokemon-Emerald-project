
using System;

using UnityEngine;

public class NpcLogic : MonoBehaviour,IInjectable
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

    private Dialogue_handler _dialogueHandler;
    private Player_movement _playerMovement;
    private Options_manager _dialogueOptionsHandler;
    public void Inject(ServiceContainer container)
    {
        _dialogueOptionsHandler = container.Resolve<Options_manager>();
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
        
        _playerMovement.FaceOppositeDirection(movementHandler.GetCurrentDirection());
        
        //positions are always locked to whole numbers
        var distance = (int)Vector3.Distance(transform.position, _playerMovement.GetPlayerPosition());

        if (distance>1)
        {
            _longDistanceDetection = true;
            _runDialogueInteraction = () => _dialogueHandler.StartInteraction(npcInteraction);
            movementHandler.OnMovementEnded += _runDialogueInteraction;
            StartCoroutine(movementHandler.MoveToSpecific(movementHandler.GetCurrentDirection(),distance-1));
        }
        else
        {
            movementHandler.StopMovement(false);
            _dialogueHandler.StartInteraction(npcInteraction);
        }
    }
}
