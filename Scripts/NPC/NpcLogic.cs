
using UnityEngine;

public class NpcLogic : MonoBehaviour
{
    public NpcMovement movementHandler;
    public bool autoDetectPlayer;
    [SerializeField]private float detectDistance = 1f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField]private Interaction npcInteraction;
    public Transform rayCastPoint;
    private bool constantScan;
    private Vector2 idleDirection;
    private bool playerDetected;
    
    private void Start()
    {
        playerDetected = false;
        if (autoDetectPlayer)
        {
            if (movementHandler.animationData.isIdle)
            {
                idleDirection = movementHandler.GetDirectionAsVector();
                constantScan = true;
            }
            else
            {
                movementHandler.OnMovementPaused += DetectPlayer;
            }
        }
        Options_manager.Instance.OnInteractionOptionChosen += PauseForInteraction;
    }

    private void Update()
    {
        if (!constantScan || playerDetected)
            return;

        DetectPlayer(idleDirection);
    }

    private void PauseForInteraction(Interaction interaction,int optionChosen)
    {
        if(interaction.overworldInteraction!=OverworldInteractionType.Battle)return;
        if(interaction!=npcInteraction)return;
        if (playerDetected)
        {
            Player_movement.Instance.FaceOppositeDirection(movementHandler.GetCurrentDirection());
        }else movementHandler.FacePlayerDirection();

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
        
        Dialogue_handler.Instance.StartInteraction(npcInteraction);
    }
}
