using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Animation_manager : MonoBehaviour
{
    public Animator animator;
    private string _currentState;
    //states
    public string playerWalk = "Player_walk";
    public string playerIdle = "Player_idle";
    public string playerRun = "Player_run";
    public string rideBike = "Ride_Bike";
    public string bikeIdle = "Bike_idle";
    public string fishingStart = "Fishing_Start";
    public string fishingEnd = "Fishing_End";
    public string fishingIdle = "Fishing_idle";
    public string movementDirectionParameter = "Movement Direction";
    public string idleDirectionParameter = "Idle Direction";
    public void ChangeAnimationState(string newState)
    {
        if (_currentState == newState) return;
        animator.Play(newState);
        _currentState = newState;
    }

}
