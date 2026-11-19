using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public abstract class StoryObjective : ScriptableObject
{
    public void LoadObjective(ServiceContainer container)
    {
        serviceContainer = container;
        if (playerBoundary.isRestricting)
        {
            serviceContainer.Resolve<PlayerMovementHandler>().SetPositionBoundary(playerBoundary);
        }
        
        serviceContainer.Resolve<InteractionHandler>().SetInteractionRestrictions(restrictedInteractions);
        
        OnLoad?.Invoke();
        OnObjectiveLoaded();
    }
    public void ClearObjective()
    {
        if (playerBoundary.isRestricting)
        {
            serviceContainer.Resolve<PlayerMovementHandler>().RemovePositionBoundary();
        }
        
        serviceContainer.Resolve<InteractionHandler>().ClearRestrictions();
        
        OnClear?.Invoke();
        OnObjectiveCleared();
    }
    
    protected virtual void OnObjectiveCleared() { }
    protected virtual void OnObjectiveLoaded() { }
    
    public event Action OnLoad;
    public event Action OnClear;
    
    public string objectiveHeading;
    public StoryObjectiveType objectiveType;
    public PlayerBoundary playerBoundary;
    public List<OverworldInteractionType> restrictedInteractions = new();
    protected ServiceContainer serviceContainer;
}
public enum StoryObjectiveType
{
    Placeholder,Interaction,WildBattle,GeneralItemUiUsage,StoryProgress,
    MarketUiUsage,BerryInteraction,PokemonStorageUiUsage,TrainerBattle,GiftPokemon,
    PickupItem,NpcInteraction,NpcMovement
}