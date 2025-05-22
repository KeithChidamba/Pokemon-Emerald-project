using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Player_movement : MonoBehaviour
{
    public float movementSpeed = 0f;
    [SerializeField] float walkSpeed = 0.8f;
    [SerializeField] float runSpeed = 1.5f;
    private const float BikeSpeed = 2f; 
    public bool runningInput;
    public bool walking;
    public bool movingOnFoot;
    public bool usingBike = false;
    public bool canUseBike = true;
    private bool _canSwitchMovement; 
    [SerializeField] private float xAxisInput;
    [SerializeField] private float yAxisInput;
    public Rigidbody2D rb;
    [SerializeField] private Vector2 movement;
    private float _currentDirection = 0;
    private Animation_manager _animationManager;
    public bool canMove = true;
    [SerializeField] Transform interactionPoint;
    public static Player_movement Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _animationManager = GetComponent<Animation_manager>();
    }
    private void Update()
    {
        if (canMove)
        {
            _animationManager.animator.SetFloat(_animationManager.idleDirectionParameter, _currentDirection);
            _animationManager.animator.SetFloat(_animationManager.movementDirectionParameter, GetMovementDirection());
            HandleBikeInputs();
            HandleRunInputs();
            HandlePlayerPhysicsMovement();
            MovementWithBikeLogic();
            MovementOnFootLogic();
        }
        else
            DisablePlayerMovement();
    }

    private void DisablePlayerMovement()
    {
        rb.velocity = Vector2.zero;
        walking = false;
        movingOnFoot = false;
        if (overworld_actions.Instance.usingUI)
            _animationManager.ChangeAnimationState(_animationManager.playerIdle);
        if (!overworld_actions.Instance.doingAction && !overworld_actions.Instance.usingUI) //dont want to interrupt fishing animation
            _animationManager.ChangeAnimationState(_animationManager.playerIdle);
    }
    private float GetMovementDirection()
    {
        float direction = 0;
        yAxisInput = (xAxisInput != 0)? 0 : yAxisInput;
        xAxisInput = (yAxisInput != 0)? 0 : xAxisInput;
        var idle = yAxisInput == 0 && xAxisInput == 0;
        
        if (idle)
        {
            if( !usingBike && !overworld_actions.Instance.doingAction)
            {
                direction = 0;
                movingOnFoot = false;
                _animationManager.ChangeAnimationState(_animationManager.playerIdle);
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
        if (direction != 0)
            _currentDirection = direction;

        return direction; 
    }

    private void HandleRunInputs()
    {
        if (usingBike) return;
        if (Input.GetKeyDown(KeyCode.LeftShift) && !runningInput)
        {
            runningInput = true;
            walking = false;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) && runningInput)
            _canSwitchMovement = true;
        
        if (Input.GetKeyDown(KeyCode.LeftShift) && runningInput && _canSwitchMovement)
        {
            runningInput = false;
            _canSwitchMovement = false;
        }
    }
    private void HandleBikeInputs()
    {
        if (Input.GetKeyDown(KeyCode.E) && !usingBike &&canUseBike)
        {
            usingBike = true;
            movingOnFoot = false;
            runningInput = false;
            walking = false;
            _canSwitchMovement = false;
        }       
        else if (Input.GetKeyDown(KeyCode.E) && !canUseBike)
            Dialogue_handler.Instance.DisplayInfo("Cant use bike here","Details",1f);
        
        if (Input.GetKeyUp(KeyCode.E) && usingBike)
            _canSwitchMovement = true;
        
        if (Input.GetKeyDown(KeyCode.E) && usingBike && _canSwitchMovement)
        {
            usingBike = false;
            _canSwitchMovement = false;
        }
    }
    
    private void MovementOnFootLogic()
    {
        if (usingBike) return;
        movementSpeed = runningInput? runSpeed : walkSpeed;
        movingOnFoot = rb.velocity != Vector2.zero;
        
        if(runningInput)
        {
            var animationName = (movingOnFoot) ? _animationManager.playerRun : _animationManager.playerIdle;
            _animationManager.ChangeAnimationState(animationName);
        }
        if (movingOnFoot && !runningInput && !_canSwitchMovement)
        {
            walking = true; 
            _animationManager.ChangeAnimationState(_animationManager.playerWalk);
        }
        else
            walking = false;
    }
    private void MovementWithBikeLogic() 
    {
        if(!usingBike)return; 
        movementSpeed = BikeSpeed;
        var idleWithBike = yAxisInput == 0 && xAxisInput == 0;
        var animationName = (idleWithBike)? _animationManager.bikeIdle : _animationManager.rideBike;
        _animationManager.ChangeAnimationState(animationName);
    }
    private void HandlePlayerPhysicsMovement()
    {
        var movementDirection = GetMovementDirection(); 
        var horizontalDirection = (movementDirection > 2) ? 1 : 0;
        var verticalDirection = (movementDirection > 2) ? 0 : 1;
        xAxisInput = Input.GetAxisRaw("Horizontal");
        yAxisInput = Input.GetAxisRaw("Vertical");
        movement = new Vector2(xAxisInput * movementSpeed * horizontalDirection, yAxisInput * movementSpeed * verticalDirection);
        rb.velocity = movement;
    }
}
