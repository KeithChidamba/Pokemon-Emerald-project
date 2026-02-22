using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_movement : MonoBehaviour
{
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
    [SerializeField] private Animation_manager _animationManager;
    [SerializeField] private bool canMove = true;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private Transform movePoint;
    private bool _delayingMovement;
    [SerializeField] private GameObject playerObject;
    public event Action OnNewTile;
    [SerializeField]private LayerMask movementBlockers;
    [SerializeField]private bool standingOnTile;
    public static Player_movement Instance;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        movementBlockers = 1 << LayerMask.NameToLayer("Movement blockers");
        overworld_actions.Instance.OnItemEquipped +=
            (item) => StopBikeUsage(item != Equipable.Bike);
        overworld_actions.Instance.OnItemUnequipped +=
            (item) => StopBikeUsage(item == Equipable.Bike);
    }

    public void FaceOppositeDirection(MovementDirection direction)
    {
        canMove = false;
        var pos = NpcMovement.SnapToGrid(playerObject.transform.position);
        playerObject.transform.position = pos;
        standingOnTile = true;
        
        currentDirection = direction switch
        {
            MovementDirection.Down => MovementDirection.Up,
            MovementDirection.Up => MovementDirection.Down,
            MovementDirection.Left => MovementDirection.Right,
            _ => MovementDirection.Left
        };
        var directionAsAnimatorParameter = (int)currentDirection;
        
        _animationManager.animator.SetFloat(_animationManager.idleParam, directionAsAnimatorParameter);
        _animationManager.animator.SetFloat(_animationManager.moveParam, directionAsAnimatorParameter);

        _animationManager.ChangeAnimationState(_animationManager.playerIdle);
    }
    
    public void AllowPlayerMovement()
    {
        if (_delayingMovement) return;
        if (!usingBike) ForceWalkMovement();
        canMove = true;
        SetCurrentAnimation();
    }

    public IEnumerator AllowPlayerMovement(float delay)
    {
        _delayingMovement = true;
        yield return new WaitForSeconds(delay);
        _delayingMovement = false;
        AllowPlayerMovement();
    }

    public void RestrictPlayerMovement()
    {
        canMove = false;
        playerObject.transform.position = movePoint.position;
        standingOnTile = true;
        
        if (overworld_actions.Instance.doingAction && overworld_actions.Instance.fishing)
        {
            //dont want to interrupt fishing animation
            return; 
        }
        _animationManager.ChangeAnimationState(_animationManager.playerIdle);
    }

    private void Update()
    {
        if (!playerObject.activeSelf) return;
        if (canMove)
        {
            _animationManager.animator.SetFloat(_animationManager.idleParam, (int)currentDirection);
            _animationManager.animator.SetFloat(_animationManager.moveParam, (int)GetMovementDirection());
            HandleBikeInputs();
            HandleRunInputs();
            HandlePlayerMovement();
        }
    }

    private void SetCurrentAnimation()
    {
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if (usingBike)
        {
            _animationManager.ChangeAnimationState(idle
                ? _animationManager.bikeIdle
                : _animationManager.rideBike);
            return;
        }

        if (idle)
        {
            _animationManager.ChangeAnimationState(_animationManager.playerIdle);
        }
        else
        {
            _animationManager.ChangeAnimationState(runningInput
                ? _animationManager.playerRun
                : _animationManager.playerWalk);
        }
    }
    public void ForceWalkMovement()
    {
        usingBike = false;
        runningInput = false;
        _canSwitchMovement = false;
    }

    private MovementDirection GetMovementDirection()
    {
        MovementDirection direction = 0;
        yAxisInput = (xAxisInput != 0) ? 0 : yAxisInput;
        xAxisInput = (yAxisInput != 0) ? 0 : xAxisInput;
        var idle = yAxisInput == 0 && xAxisInput == 0;

        if (idle)
        {
            if (!overworld_actions.Instance.doingAction)
            {
                return currentDirection;
            }
        }
        else
        {
            if (yAxisInput != 0)
            {
                var verticalRotation = (yAxisInput > 0) ? -90 : 90;
                direction = (yAxisInput > 0) ? MovementDirection.Up : MovementDirection.Down;
                interactionPoint.rotation = Quaternion.Euler(verticalRotation, 0, 0);
            }

            if (xAxisInput != 0)
            {
                var horizontalRotation = (xAxisInput > 0) ? 90 : -90;
                direction = (xAxisInput > 0) ? MovementDirection.Right : MovementDirection.Left ;
                interactionPoint.rotation = Quaternion.Euler(0, horizontalRotation, 0);
            }
        }

        currentDirection = direction;

        return direction;
    }

    private void HandleRunInputs()
    {
        if (usingBike) return;
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if (Input.GetKeyDown(KeyCode.X) && !runningInput)
        {
            runningInput = true;
            if (!idle) _animationManager.ChangeAnimationState(_animationManager.playerRun);
        }

        if (Input.GetKeyUp(KeyCode.X) && runningInput)
            _canSwitchMovement = true;

        if (Input.GetKeyDown(KeyCode.X) && runningInput && _canSwitchMovement)
        {
            runningInput = false;
            _canSwitchMovement = false;
            if (!idle) _animationManager.ChangeAnimationState(_animationManager.playerWalk);
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
        if (!overworld_actions.Instance.IsEquipped(Equipable.Bike)) return;

        if (Input.GetKeyDown(KeyCode.C) && !usingBike && canUseBike)
        {
            usingBike = true;
            runningInput = false;
            _canSwitchMovement = false;
            movementSpeed = BikeSpeed;
            SetCurrentAnimation();
        }
        else if (Input.GetKeyDown(KeyCode.C) && !canUseBike)
            Dialogue_handler.Instance.DisplayDetails("Cant use bike here");

        if (Input.GetKeyUp(KeyCode.C) && usingBike)
            _canSwitchMovement = true;

        if (Input.GetKeyDown(KeyCode.C) && usingBike && _canSwitchMovement)
        {
            usingBike = false;
            _canSwitchMovement = false;
            SetCurrentAnimation();
        }
    }

    public Vector2 GetDirectionAsVector2()
    {
        var currentDirectionIndex = (int)currentDirection;
        
        // 1-down:   2-up:   3-left: 4-right
        List<Vector2> directionConversions = new (){ new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };
        
        return directionConversions[currentDirectionIndex]; 
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
                OnNewTile?.Invoke();
                standingOnTile = true;
            }
            yAxisInput = (int)Input.GetAxisRaw("Vertical");
            xAxisInput = (int)Input.GetAxisRaw("Horizontal");
            
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

    public void ActivatePlayerFromSave(Vector3 position)
    {
        SetPlayerPosition(position);
        playerObject.SetActive(true);
    }
    public void SetPlayerPosition(Vector3 position)
    {
        movePoint.position = position;
        playerObject.transform.position = position;
    }
    public Vector3 GetPlayerPosition()
    {
        return playerObject.transform.position;
    }
}
