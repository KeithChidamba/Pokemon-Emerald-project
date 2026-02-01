using System;
using UnityEngine;
[Serializable]
public abstract class StoryObjective : ScriptableObject
{
    private void LoadObjective()
    {
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
    
    public void FindMainAsset()
    {
        var mainAsset = Resources.Load<StoryObjective>(Save_manager.GetDirectory(AssetDirectory.StoryObjectiveData)+mainAssetName);
        if (mainAsset == null)
        {
            Debug.LogWarning("Story objective Asset: "+mainAssetName+" not found");
            return;
        }
        if(hasProgression) mainAsset.LoadSaveData(this);
        mainAsset.LoadObjective();
    }
    public event Action OnLoad;
    public event Action OnClear;
    public string mainAssetName;
    public string objectiveHeading;
    public bool hasProgression;
    public int indexInList;
    public StoryObjectiveType objectiveType;

    public static StoryObjective GetObjectiveType(StoryObjectiveType type)
    {
        return type switch
        {
            StoryObjectiveType.Destination => CreateInstance<DestinationObjective>(),
            StoryObjectiveType.StoryProgress => CreateInstance<StoryProgressObjective>(),
            StoryObjectiveType.MarketUiUsage => CreateInstance<MarketUiObjective>(),
            StoryObjectiveType.GeneralItemUiUsage => CreateInstance<GeneralItemUiObjective>(),
            StoryObjectiveType.PokemonStorageUiUsage => CreateInstance<PokemonStorageObjective>(),
            StoryObjectiveType.Interaction => CreateInstance<InteractionObjective>(),
            StoryObjectiveType.BerryInteraction => CreateInstance<BerryInteractionObjective>(),
            StoryObjectiveType.WildBattle => CreateInstance<WildBattleObjective>(),
            StoryObjectiveType.TrainerBattle => CreateInstance<TrainerBattleObjective>(),
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
    Destination,Interaction,WildBattle,GeneralItemUiUsage,StoryProgress
    ,MarketUiUsage,BerryInteraction,PokemonStorageUiUsage,TrainerBattle
}