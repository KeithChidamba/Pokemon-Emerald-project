using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class overworld_actions : MonoBehaviour
{
    public Animation_manager manager;
    float bike_speed = 2f;
    public bool canSwitch = false;
    public bool fishing = false;
    public bool doing_action = false;
    public bool using_ui = false;
    public static overworld_actions instance;
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
        if (!using_ui)
        {
            if (doing_action)
            {
                Player_movement.instance.canmove = false;
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
            Player_movement.instance.canmove = false;
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
        Dialogue_handler.instance.Dialouge_off();
    }
    void Action_reset()
    {
        doing_action = false;
        Player_movement.instance.canmove = true;
    }
    public void Use_Bike()
    {
        Player_movement.instance.movement_speed = bike_speed;
    }
}
