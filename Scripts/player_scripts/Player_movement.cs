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
    public bool running;
    public bool walking;
    public bool moving;
    public bool usingBike = false;
    public bool canUseBike = true;
    [SerializeField] float x, y;
    public Rigidbody2D rb;
    [SerializeField] Vector2 movement;
    float _currentDirection = 0;
    public Animation_manager manager;
    public bool canMove = true;
    [SerializeField] Transform interactionPoint;
    public static Player_movement Instance { get; private set;}
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Update()
    {
        if (canMove)
        {
            manager.animator.SetFloat("idleDir", _currentDirection);
            manager.animator.SetFloat("Movement_direction", Get_Direction());
            Inputs();
            Move();
            Bike_logic();
            if (!usingBike)
            {
                Move_Logic();
            }
            else
            {
                running = false;
                walking = false;
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
            running = false;
            walking = false;
            usingBike = false;
            overworld_actions.Instance.canSwitchMovement = false;
            moving = false;
            if (overworld_actions.Instance.usingUI)
                manager.change_animation_state(manager.playerIdle);
            if (!overworld_actions.Instance.doingAction && !overworld_actions.Instance.usingUI)
                manager.change_animation_state(manager.playerIdle);
        }
    }
    float Get_Direction()
    {
        float direction = 0;
        if (x != 0 )
        {
            y = 0;
        }
        if(y != 0)
        {
            x = 0;
        }
        if (y == 0 && x == 0 && !usingBike && !overworld_actions.Instance.doingAction)
        {
            direction = 0;
            moving = false;
            manager.change_animation_state(manager.playerIdle);
        }
        else
        {
            if (y > 0)
            {
                direction = 2;
                interactionPoint.rotation = Quaternion.Euler(-90, 0, 0);
            }
            if (y < 0)
            {
                direction = 1;
                interactionPoint.rotation = Quaternion.Euler(90, 0, 0);
            }
            if (x < 0)
            {
                direction = 3;
                interactionPoint.rotation = Quaternion.Euler(0, -90, 0);
            }
            if (x > 0)
            {
                direction = 4;
                interactionPoint.rotation = Quaternion.Euler(0, 90, 0);
            }
        }
        if (direction != 0)
        {
            _currentDirection = direction;
        }

        return direction; 
    }
    void Inputs()
    {
        if (Input.GetKeyDown(KeyCode.E) && !usingBike &&canUseBike)
        {
            overworld_actions.Instance.SetBikeMovementSpeed();
            usingBike = true;
            moving = false;
            running = false;
            overworld_actions.Instance.canSwitchMovement = false;
        }       
        else if (Input.GetKeyDown(KeyCode.E) && !canUseBike)
        {
            Dialogue_handler.Instance.DisplayInfo("Cant use bike here","Details",1f);
        }
        if (Input.GetKeyUp(KeyCode.E) && usingBike)
        {
            overworld_actions.Instance.canSwitchMovement = true;
        }
        if (Input.GetKeyDown(KeyCode.E) && usingBike && overworld_actions.Instance.canSwitchMovement)
        {
            usingBike = false;
            overworld_actions.Instance.canSwitchMovement = false;
        }
        if (!usingBike)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && !running)
            {
                running = true;
                walking = false;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift) && running)
            {
                overworld_actions.Instance.canSwitchMovement = true;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) && running && overworld_actions.Instance.canSwitchMovement)
            {
                running = false;
                overworld_actions.Instance.canSwitchMovement = false;
            }
            if (!moving)
            {
                walking = false;
            }
            if (running)
            {
                if (!moving)
                {
                    manager.change_animation_state(manager.playerIdle);
                }
                else
                {
                    manager.change_animation_state(manager.playerRun);
                }
            }
            if (moving && !running && !overworld_actions.Instance.canSwitchMovement)
            {
                walking = true; 
                manager.change_animation_state(manager.playerWalk);
            }
            else
            {
                walking = false;
            }
        }
    }
    void Move_Logic()
    {
        if (running)
        {
            movementSpeed = runSpeed;
        }
        else
        {
            movementSpeed = walkSpeed;
        }
        if (rb.velocity != Vector2.zero)
        {
            moving = true;
        }
        else
        {
            moving = false;
        }

    }
    void Bike_logic()
    {
        if (y == 0 && x == 0 && usingBike)
        {
            manager.change_animation_state(manager.bikeIdle);
        }
        if ((y != 0 || x != 0) && usingBike)
        {
            manager.change_animation_state(manager.rideBike);
        }
    }
    void Move()
    {
        
        int move_check_x;
        int move_check_y;

        if (Get_Direction() > 2)
        {
            move_check_x = 1;
            move_check_y = 0;
        }
        else
        {
            move_check_x = 0;
            move_check_y = 1;
        }
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        movement = new Vector2(x * movementSpeed * move_check_x, y * movementSpeed * move_check_y);
        rb.velocity = movement;

    }
}
