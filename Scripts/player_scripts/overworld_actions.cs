using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class overworld_actions : MonoBehaviour
{
    public Player_movement movement;
    public Animation_manager manager;
    public Dialogue_handler dialogue;
    float bike_speed = 2f;
    public bool canSwitch = false;
    public bool fishing = false;
    public bool doing_action = false;
    public bool using_ui = false;
    void Update()
    {
        if (!using_ui)
        {
            if (doing_action)
            {
                movement.canmove = false;
                canSwitch = false;
            }
            if (fishing)
            {
                doing_action = true;
                manager.change_animation_state(manager.Fishing_idle);
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    Done_fishing();
                }
            }
        }
        else
        {
            movement.canmove = false;
        }

    }
    void Start_fishing()
    {
        fishing = true;
    }
    void Done_fishing()
    {
        fishing = false;
        Invoke(nameof(Action_reset), 0.8f);
        manager.change_animation_state(manager.Fishing_End);
        dialogue.Dialouge_off();
    }
    void Action_reset()
    {
        doing_action = false;
        movement.canmove = true;
    }
    public void Use_Bike()
    {
        movement.movement_speed = bike_speed;
    }
}
