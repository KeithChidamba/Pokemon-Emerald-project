using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
public enum MovementRestrictor{Dialogue,UI,OverworldAction,Battle}
public class Player_movement : MonoBehaviour,IInjectable
{
    public Camera playerCamera;
    public float movementSpeed;
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 6f;
    private const float BikeSpeed = 10f;
    public bool runningInput;
    public bool usingBike;
    public bool canUseBike = true;
    private bool _canSwitchMovement;
    [SerializeField] private int xAxisInput;
    [SerializeField] private int yAxisInput;
    public MovementDirection currentDirection;
    [SerializeField] private Animation_manager animationManager;
    [SerializeField] private bool canMove = true;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform movePoint;

    [SerializeField] private GameObject playerObject;
    public event Action OnNewTile;
    public SpriteRenderer characterSpriteMaskRenderer;
    [SerializeField] private SpriteRenderer characterMainSpriteRenderer;
    public Vector3 PreviousValidPosition { get; private set; }
    [SerializeField]private LayerMask movementBlockers;
    [SerializeField]private bool standingOnTile;
    private Dictionary<MovementRestrictor, bool> _movementRestrictors = new();
    
    private overworld_actions _overworldActions;
    private Dialogue_handler _dialogueHandler;
    
    public void Inject(ServiceContainer container)
    {
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _overworldActions = container.Resolve<overworld_actions>();
        gameObject.SetActive(true);
    }

    public void OnInject()
    {
        _movementRestrictors.Add(MovementRestrictor.Battle,false);
        _movementRestrictors.Add(MovementRestrictor.Dialogue,false);
        _movementRestrictors.Add(MovementRestrictor.UI,false);
        _movementRestrictors.Add(MovementRestrictor.OverworldAction,false);
        
        _overworldActions.OnItemEquipped +=
            (item) => StopBikeUsage(item != Equipable.Bike);
        _overworldActions.OnItemUnequipped +=
            (item) => StopBikeUsage(item == Equipable.Bike);
        
        _dialogueHandler.OnDialogueEnded += () => AllowPlayerMovement(MovementRestrictor.Dialogue,0.75f);
        _overworldActions.OnActionComplete += () => AllowPlayerMovement(MovementRestrictor.OverworldAction,0.75f);
    }

    private void SnapToPosition()
    {
        canMove = false;
        standingOnTile = true;
        playerObject.transform.position = PreviousValidPosition;
        movePoint.position = PreviousValidPosition;
    }
    public void FaceOppositeDirection(MovementDirection npcDirection)
    {
        SnapToPosition();
        ForceWalkMovement();
        currentDirection = npcDirection switch
        {
            MovementDirection.Down => MovementDirection.Up,
            MovementDirection.Up => MovementDirection.Down,
            MovementDirection.Left => MovementDirection.Right,
            _ => MovementDirection.Left
        };
        
        var directionAsAnimatorParameter = (int)currentDirection;
        
        animationManager.animator.SetFloat(animationManager.idleParam, directionAsAnimatorParameter);
        animationManager.ChangeAnimationState(PlayerAnimationState.PlayerIdle);
        animationManager.animator.SetFloat(animationManager.moveParam, directionAsAnimatorParameter);
        
    }
    public Vector2 GetPlayerDirectionAsVector2()
    {
        var currentDirectionIndex = (int)currentDirection;
        
        // 1-down:   2-up:   3-left: 4-right
        List<Vector2> directionConversions = new (){ new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };
        
        return directionConversions[currentDirectionIndex==0? 0 : currentDirectionIndex-1]; 
    }

    public void AllowPlayerMovement(MovementRestrictor restrictor,float delay=1f)
    {
        if (!_movementRestrictors[restrictor]) return;
        
        StartCoroutine(MovementAllowanceDelay());
        IEnumerator MovementAllowanceDelay()
        {
            _movementRestrictors[restrictor] = false;
            yield return new WaitForSeconds(delay);
            if (_movementRestrictors.Any(r => r.Value))
            {
                yield break;
            }
            if (!usingBike) ForceWalkMovement();
            canMove = true;
            SetCurrentAnimation();
        }
    }
    
    public void RestrictPlayerMovement(MovementRestrictor restrictor)
    {
        if (_movementRestrictors[restrictor]) return;
        SnapToPosition();
        _movementRestrictors[restrictor] = true;
        if (_overworldActions.fishing)
        {
            //dont want to interrupt fishing animation
            return; 
        }
        animationManager.ChangeAnimationState(PlayerAnimationState.PlayerIdle);
    }

    private void Update()
    {
        if (!playerObject.activeSelf) return;
        if (canMove)
        {
            animationManager.animator.SetFloat(animationManager.idleParam, (int)currentDirection);
            animationManager.animator.SetFloat(animationManager.moveParam, (int)GetMovementDirection());
            HandleBikeInputs();
            HandleRunInputs();
            HandlePlayerMovement();
            characterSpriteMaskRenderer.sprite = characterMainSpriteRenderer.sprite;
            characterSpriteMaskRenderer.transform.rotation = characterMainSpriteRenderer.transform.rotation;
        }
        else
        {
            yAxisInput = 0;
            xAxisInput = 0;
        }
    }

    private void SetCurrentAnimation()
    {
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if (usingBike)
        {
            animationManager.ChangeAnimationState(idle
                ? PlayerAnimationState.BikeIdle
                : PlayerAnimationState.RideBike);
            return;
        }

        if (idle)
        {
            animationManager.ChangeAnimationState(PlayerAnimationState.PlayerIdle);
        }
        else
        {
            animationManager.ChangeAnimationState(runningInput
                ? PlayerAnimationState.PlayerRun
                : PlayerAnimationState.PlayerWalk);
        }
    }
    private void ForceWalkMovement()
    {
        usingBike = false;
        runningInput = false;
        _canSwitchMovement = false;
    }

    private MovementDirection GetMovementDirection()
    {
        yAxisInput = (xAxisInput != 0) ? 0 : yAxisInput;
        xAxisInput = (yAxisInput != 0) ? 0 : xAxisInput;
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if (!idle)
        {
            if (yAxisInput != 0)
            {
                var verticalRotation = (yAxisInput > 0) ? -90 : 90;
                currentDirection = (yAxisInput > 0) ? MovementDirection.Up : MovementDirection.Down;
                interactionPoint.rotation = Quaternion.Euler(verticalRotation, 0, 0);
            }

            if (xAxisInput != 0)
            {
                var horizontalRotation = (xAxisInput > 0) ? 90 : -90;
                currentDirection = (xAxisInput > 0) ? MovementDirection.Right : MovementDirection.Left ;
                interactionPoint.rotation = Quaternion.Euler(0, horizontalRotation, 0);
            }
        }
        return currentDirection;
    }

    private void HandleRunInputs()
    {
        if (usingBike) return;
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if (InputSourceHandler.InputPressed(ControlEvent.Exit) && !runningInput)
        {
            runningInput = true;
            if (!idle) animationManager.ChangeAnimationState(PlayerAnimationState.PlayerRun);
        }

        if (InputSourceHandler.InputRelease(ControlEvent.Exit) && runningInput)
            _canSwitchMovement = true;

        if (InputSourceHandler.InputPressed(ControlEvent.Exit) && runningInput && _canSwitchMovement)
        {
            runningInput = false;
            _canSwitchMovement = false;
            if (!idle) animationManager.ChangeAnimationState(PlayerAnimationState.PlayerWalk);
        }

        movementSpeed = runningInput ? runSpeed : walkSpeed;
    }

    private void StopBikeUsage(bool canStopBikeUsage)
    {
        if (!canStopBikeUsage) return;
        usingBike = false;
        _canSwitchMovement = false;
    }

    private void HandleBikeInputs()
    {
        if (!_overworldActions.IsEquipped(Equipable.Bike)) return;

        if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem) && !usingBike && canUseBike)
        {
            usingBike = true;
            runningInput = false;
            _canSwitchMovement = false;
            movementSpeed = BikeSpeed;
            SetCurrentAnimation();
        }
        else if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem) && !canUseBike)
            _dialogueHandler.DisplayDetails("Cant use bike here");

        if (InputSourceHandler.InputRelease(ControlEvent.UseSpecialItem)&& usingBike)
            _canSwitchMovement = true;

        if (InputSourceHandler.InputPressed(ControlEvent.UseSpecialItem) && usingBike && _canSwitchMovement)
        {
            usingBike = false;
            _canSwitchMovement = false;
            SetCurrentAnimation();
        }
    }


    private void HandlePlayerMovement()
    {
        playerObject.transform.position = Vector3.MoveTowards(playerObject.transform.position, movePoint.position,
            movementSpeed * Time.deltaTime);
        
        var inputSwitchRange = 0.05f;//controls the distance at which the player can switch inputs
        
        if (Vector3.Distance(playerObject.transform.position, movePoint.position) <= inputSwitchRange)
        {
            playerObject.transform.position = movePoint.position;
            if (!standingOnTile)
            {
                PreviousValidPosition = movePoint.position;
                standingOnTile = true;
                OnNewTile?.Invoke();
            }
            
            yAxisInput = GetAxisFromInput(ControlEvent.Down, ControlEvent.Up);
            xAxisInput = GetAxisFromInput(ControlEvent.Left, ControlEvent.Right);
            
//prevent diagonal movent and opposite inputs
            bool verticalInput = Math.Abs(yAxisInput) == 1;
            bool horizontalInput = Math.Abs(xAxisInput) == 1;
            
            if (verticalInput)
            {
                xAxisInput = 0;
            }
            else if (horizontalInput)
            {
                yAxisInput = 0;
            }
            
//movement
            if (Math.Abs(yAxisInput) == 1)
            {
                var positionModifierY = new Vector3(0, yAxisInput, 0);
                var hit = Physics2D.Raycast(
                    interactionPoint.transform.position,
                    positionModifierY,
                    1f,movementBlockers
                );
                if (!hit)
                {//check blockers
                    movePoint.position += positionModifierY;
                    standingOnTile = false;
                }
            }
            
            if (Math.Abs(xAxisInput) == 1)
            {
                var positionModifierX = new Vector3(xAxisInput, 0, 0);
                var hit = Physics2D.Raycast(
                    interactionPoint.transform.position,
                    positionModifierX,
                    1f,movementBlockers
                );
                if (!hit)
                {//check blockers
                    movePoint.position += positionModifierX;
                    standingOnTile = false;
                }
            }

            SetCurrentAnimation();
        }
    }
    int GetAxisFromInput(ControlEvent negative, ControlEvent positive)
    {
        bool neg = InputSourceHandler.InputHeld(negative);
        bool pos = InputSourceHandler.InputHeld(positive);

        if (neg == pos) return 0; // both pressed OR neither
        return pos ? 1 : -1;
    }
    public void ActivatePlayerFromSave(Vector3 position)
    {
        SetPlayerPosition(position);
        playerObject.SetActive(true);
    }
    public void SetPlayerPosition(Vector3 position)
    {
        PreviousValidPosition = position;
        movePoint.position = position;
        playerObject.transform.position = position;
    }
    public Vector3 GetPlayerPosition()
    {
        return playerObject.transform.position;
    }
}
