using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation_manager : MonoBehaviour
{
    public Animator animator;
    private string current_state;
    //states
    public string Player_walk = "Player_walk";
    public string Player_idle = "Player_idle";
    public string Player_run = "Player_run";
    public string Ride_Bike = "Ride_Bike";
    public string Bike_idle = "Bike_idle";
    public string Fishing_Start = "Fishing_Start";
    public string Fishing_End = "Fishing_End";
    public string Fishing_idle = "Fishing_idle";

    public void change_animation_state(string new_state)
    {
        if (current_state == new_state) return;
        animator.Play(new_state);
        current_state = new_state;
    }

}
