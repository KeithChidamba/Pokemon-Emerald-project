using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player_movement : MonoBehaviour
{
    public float movement_speed = 0f;
    [SerializeField] float walk_speed = 0.8f;
    [SerializeField] float run_speed = 1.5f;
    public bool running;
    public bool walking;
    public bool moving;
    public bool using_bike = false;
    public bool can_use_bike = true;
    [SerializeField] float x, y;
    public Rigidbody2D rb;
    [SerializeField] Vector2 movement;
    float current_direction = 0;
    public Animation_manager manager;
    public bool canmove = true;
    [SerializeField] Transform interaction_point;
    public static Player_movement instance { get; private set;}
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    void Update()
    {
        if (canmove)
        {
            manager.animator.SetFloat("idleDir", current_direction);
            manager.animator.SetFloat("Movement_direction", Get_Direction());
            Inputs();
            Move();
            Bike_logic();
            if (!using_bike)
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
            using_bike = false;
            overworld_actions.Instance.canSwitchMovement = false;
            moving = false;
            if (overworld_actions.Instance.usingUI)
                manager.change_animation_state(manager.Player_idle);
            if (!overworld_actions.Instance.doingAction && !overworld_actions.Instance.usingUI)
                manager.change_animation_state(manager.Player_idle);
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
        if (y == 0 && x == 0 && !using_bike && !overworld_actions.Instance.doingAction)
        {
            direction = 0;
            moving = false;
            manager.change_animation_state(manager.Player_idle);
        }
        else
        {
            if (y > 0)
            {
                direction = 2;
                interaction_point.rotation = Quaternion.Euler(-90, 0, 0);
            }
            if (y < 0)
            {
                direction = 1;
                interaction_point.rotation = Quaternion.Euler(90, 0, 0);
            }
            if (x < 0)
            {
                direction = 3;
                interaction_point.rotation = Quaternion.Euler(0, -90, 0);
            }
            if (x > 0)
            {
                direction = 4;
                interaction_point.rotation = Quaternion.Euler(0, 90, 0);
            }
        }
        if (direction != 0)
        {
            current_direction = direction;
        }

        return direction; 
    }
    void Inputs()
    {
        if (Input.GetKeyDown(KeyCode.E) && !using_bike &&can_use_bike)
        {
            overworld_actions.Instance.SetBikeMovementSpeed();
            using_bike = true;
            moving = false;
            running = false;
            overworld_actions.Instance.canSwitchMovement = false;
        }       
        else if (Input.GetKeyDown(KeyCode.E) && !can_use_bike)
        {
            Dialogue_handler.Instance.DisplayInfo("Cant use bike here","Details",1f);
        }
        if (Input.GetKeyUp(KeyCode.E) && using_bike)
        {
            overworld_actions.Instance.canSwitchMovement = true;
        }
        if (Input.GetKeyDown(KeyCode.E) && using_bike && overworld_actions.Instance.canSwitchMovement)
        {
            using_bike = false;
            overworld_actions.Instance.canSwitchMovement = false;
        }
        if (!using_bike)
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
                    manager.change_animation_state(manager.Player_idle);
                }
                else
                {
                    manager.change_animation_state(manager.Player_run);
                }
            }
            if (moving && !running && !overworld_actions.Instance.canSwitchMovement)
            {
                walking = true; 
                manager.change_animation_state(manager.Player_walk);
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
            movement_speed = run_speed;
        }
        else
        {
            movement_speed = walk_speed;
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
        if (y == 0 && x == 0 && using_bike)
        {
            manager.change_animation_state(manager.Bike_idle);
        }
        if ((y != 0 || x != 0) && using_bike)
        {
            manager.change_animation_state(manager.Ride_Bike);
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
        movement = new Vector2(x * movement_speed * move_check_x, y * movement_speed * move_check_y);
        rb.velocity = movement;

    }
}
