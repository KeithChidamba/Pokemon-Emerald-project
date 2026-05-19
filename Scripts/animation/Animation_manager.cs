using System;
using UnityEngine;

public enum PlayerAnimationState
{
    PlayerWalk,
    PlayerIdle,
    PlayerRun,
    RideBike,
    BikeIdle,
    FishingStart,
    FishingEnd,
    FishingIdle,
    Watering
}

public class Animation_manager : MonoBehaviour
{
    public Animator animator;
    [SerializeField]private PlayerAnimationState currentState;
    public readonly string idleParam = "idleDirection"; 
    public readonly string moveParam = "moveDirection";
    
    
    public event Action OnFishingStart;
    
    public void StartFishing()
    {//Animation event
        OnFishingStart?.Invoke();
    }
    private string GetStateName(PlayerAnimationState state)
    {
        return state switch
        {
            PlayerAnimationState.PlayerWalk => "Player_walk",
            PlayerAnimationState.PlayerIdle => "Player_idle",
            PlayerAnimationState.PlayerRun => "Player_run",
            PlayerAnimationState.RideBike => "Ride_Bike",
            PlayerAnimationState.BikeIdle => "Bike_idle",
            PlayerAnimationState.FishingStart => "Fishing_Start",
            PlayerAnimationState.FishingEnd => "Fishing_End",
            PlayerAnimationState.FishingIdle => "Fishing_idle",
            PlayerAnimationState.Watering => "Watering",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }
    public void ChangeAnimationState(PlayerAnimationState newState)
    {
        if (currentState == newState)
        {
            Debug.Log("Dup Anim: "+newState);
            return;
        }
        Debug.Log("New Anim: "+newState);
        animator.Play(GetStateName(newState));
        currentState = newState;
    }

}
