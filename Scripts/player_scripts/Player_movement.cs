using System;
using System.Collections;
using UnityEngine;

public class Player_movement : MonoBehaviour
{
    public float movementSpeed = 0f;
    [SerializeField] float walkSpeed = 0.8f;
    [SerializeField] float runSpeed = 1.5f;
    private const float BikeSpeed = 2f; 
    public bool runningInput;
    public bool usingBike = false;
    public bool canUseBike = true;
    private bool _canSwitchMovement; 
    [SerializeField] private int xAxisInput;
    [SerializeField] private int yAxisInput;
    public Rigidbody2D rb;
    [SerializeField] private Vector2 movement;
    private float _currentDirection = 0;
    [SerializeField]private Animation_manager _animationManager;
    [SerializeField]private bool canMove = true;
    [SerializeField] Transform interactionPoint;
    private bool delayingMovement;
    public GameObject playerObject;
    public static Player_movement Instance;

    private enum InputAxis
    {
        Vertical,
        Horizontal
    };
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
        overworld_actions.Instance.OnItemEquipped += 
            (item)=> StopBikeUsage(item!=Equipable.Bike);
        overworld_actions.Instance.OnItemUnequipped += 
            (item)=> StopBikeUsage(item==Equipable.Bike);
    }

    public void AllowPlayerMovement()
    {
        if (delayingMovement) return;
        canMove = true;
        SetCurrentAnimation();
    }
    public IEnumerator AllowPlayerMovement(float delay)
    {
        delayingMovement = true;
        yield return new WaitForSeconds(delay);
        delayingMovement = false;
        canMove = true;
    }
    public void RestrictPlayerMovement()
    {
        canMove = false;
    }
    private void Update()
    {
        if (!playerObject.activeSelf) return;
        if (canMove)
        {
            _animationManager.animator.SetFloat(_animationManager.idleParam, _currentDirection);
            _animationManager.animator.SetFloat(_animationManager.moveParam, GetMovementDirection());
            HandleBikeInputs();
            HandleRunInputs();
            HandlePlayerPhysicsMovement();
        }
        else
            DisablePlayerMovement();
    }

    private void SetCurrentAnimation()
    {
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if(usingBike)
        {
            _animationManager.ChangeAnimationState(idle?_animationManager.bikeIdle
                    :_animationManager.rideBike);
            return;
        }
        if (idle)
        {
            _animationManager.ChangeAnimationState(_animationManager.playerIdle);
        }
        else
        {
            _animationManager.ChangeAnimationState(runningInput?_animationManager.playerRun
                :_animationManager.playerWalk);
        }
    }
    private void DisablePlayerMovement()
    {
        rb.velocity = Vector2.zero;
        if (overworld_actions.Instance.usingUI)
            _animationManager.ChangeAnimationState(_animationManager.playerIdle);
        if (!overworld_actions.Instance.doingAction && !overworld_actions.Instance.usingUI) //dont want to interrupt fishing animation
            _animationManager.ChangeAnimationState(_animationManager.playerIdle);
    }

    public void ForceWalkMovement()
    {
        usingBike = false;
        runningInput = false;
        _canSwitchMovement = false;
    }
    private float GetMovementDirection()
    {
        float direction = 0;
        yAxisInput = (xAxisInput != 0)? 0 : yAxisInput;
        xAxisInput = (yAxisInput != 0)? 0 : xAxisInput;
        var idle = yAxisInput == 0 && xAxisInput == 0;
        
        if (idle)
        {
            if(!usingBike && !overworld_actions.Instance.doingAction)
            {
                direction = 0;
            }
        }
        else
        {
            if (yAxisInput != 0)
            { 
                var verticalRotation = (yAxisInput > 0)? -90: 90;
                direction = (yAxisInput > 0)? 2: 1;
                interactionPoint.rotation = Quaternion.Euler(verticalRotation, 0, 0);
            }
            if (xAxisInput != 0)
            {  
                var horizontalRotation = (xAxisInput > 0)? 90: -90;
                direction = (xAxisInput > 0)? 4: 3;
                interactionPoint.rotation = Quaternion.Euler(0, horizontalRotation, 0);
            }
        }
        if (direction != 0) _currentDirection = direction;

        return direction; 
    }

    private void HandleRunInputs()
    {
        if (usingBike) return;
        var idle = yAxisInput == 0 && xAxisInput == 0;
        if (Input.GetKeyDown(KeyCode.X) && !runningInput)
        {
            runningInput = true;
            if(!idle) _animationManager.ChangeAnimationState(_animationManager.playerRun);
        }
        
        if (Input.GetKeyUp(KeyCode.X) && runningInput)
            _canSwitchMovement = true;
        
        if (Input.GetKeyDown(KeyCode.X) && runningInput && _canSwitchMovement)
        {
            runningInput = false;
            _canSwitchMovement = false;
            if(!idle) _animationManager.ChangeAnimationState(_animationManager.playerWalk);
        }
        movementSpeed = runningInput? runSpeed : walkSpeed;
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
        
        if (Input.GetKeyDown(KeyCode.C) && !usingBike &&canUseBike)
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
    private void HandlePlayerPhysicsMovement()
    {
        var newXInput = HandleInput(InputAxis.Horizontal);
        var newYInput = HandleInput(InputAxis.Vertical);
        
        if (xAxisInput != newXInput || yAxisInput != newYInput)
        {
            xAxisInput = newXInput;
            yAxisInput = newYInput;
            SetCurrentAnimation();
        }
        
        var movementDirection = GetMovementDirection();
        var horizontalDirection = (movementDirection > 2) ? 1 : 0; 
        var verticalDirection = (movementDirection > 2) ? 0 : 1;
        movement = new Vector2(xAxisInput * movementSpeed * horizontalDirection, yAxisInput * movementSpeed * verticalDirection);
        rb.velocity = movement;
    }

    private int HandleInput(InputAxis axisName)
    { 
        var inputs = axisName == InputAxis.Vertical? new []{KeyCode.DownArrow,KeyCode.UpArrow} 
            : new []{KeyCode.LeftArrow,KeyCode.RightArrow};
        
        if (Input.GetKey(inputs[0])) return -1;
        if (Input.GetKey(inputs[1])) return 1;
        return 0;
    }
}
