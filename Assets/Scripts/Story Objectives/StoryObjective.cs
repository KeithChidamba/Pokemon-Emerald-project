using System;
using UnityEngine;
[Serializable]
public abstract class StoryObjective : ScriptableObject
{
    private void LoadObjective(ServiceContainer container)
    {
        serviceContainer = container;
        OnLoad?.Invoke();
        OnObjectiveLoaded();
    }
    public void ClearObjective()
    {
        OnClear?.Invoke();
        OnObjectiveCleared();
    }
    
    protected virtual void OnObjectiveCleared() { }
    protected virtual void OnObjectiveLoaded() { }

    protected virtual void LoadSaveData(StoryObjective objectiveData){ }
    
    public void FindMainAsset(ServiceContainer container)
    {
        string dir = DirectoryHandler.GetDirectory(AssetDirectory.StoryObjectiveData);
        StoryObjective[] all = Resources.LoadAll<StoryObjective>(dir);
        var mainAsset = Array.Find(all, o => o.name == mainAssetName);
        if (mainAsset is null)
        {
            Debug.LogError("Story objective Asset: "+mainAssetName+" not found");
            return;
        }
        if(hasProgression) mainAsset.LoadSaveData(this);
        mainAsset.LoadObjective(container);
    }
    public event Action OnLoad;
    public event Action OnClear;
    public string mainAssetName;
    public string objectiveHeading;
    public bool hasProgression;
    [HideInInspector]public int indexInList;
    public StoryObjectiveType objectiveType;
    protected ServiceContainer serviceContainer;
    public static StoryObjective CreateObjectiveOfType(StoryObjectiveType type)
    {
        return type switch
        {
            StoryObjectiveType.StoryProgress => CreateInstance<StoryProgressObjective>(),
            StoryObjectiveType.MarketUiUsage => CreateInstance<MarketUiObjective>(),
            StoryObjectiveType.GeneralItemUiUsage => CreateInstance<GeneralItemUiObjective>(),
            StoryObjectiveType.PokemonStorageUiUsage => CreateInstance<PokemonStorageObjective>(),
            StoryObjectiveType.Interaction => CreateInstance<InteractionObjective>(),
            StoryObjectiveType.BerryInteraction => CreateInstance<BerryInteractionObjective>(),
            StoryObjectiveType.WildBattle => CreateInstance<WildBattleObjective>(),
            StoryObjectiveType.TrainerBattle => CreateInstance<TrainerBattleObjective>(),
            StoryObjectiveType.GiftPokemon => CreateInstance<GiftPokemonObjective>(),
            StoryObjectiveType.PickupItem => CreateInstance<ItemPickupObjective>(),
            StoryObjectiveType.NpcInteraction => CreateInstance<NpcStoryObjective>(),
            StoryObjectiveType.NpcMovement => CreateInstance<NpcMovementObjective>(),
            _ => null
        };
    }
}

[Serializable]
public class ObjectiveTypeWrapper
{
    public StoryObjectiveType objectiveType;
}
public enum StoryObjectiveType
{
    Placeholder,Interaction,WildBattle,GeneralItemUiUsage,StoryProgress,
    MarketUiUsage,BerryInteraction,PokemonStorageUiUsage,TrainerBattle,GiftPokemon,
    PickupItem,NpcInteraction,NpcMovement
}