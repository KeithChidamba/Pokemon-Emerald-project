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

    protected virtual void OnObjectiveLoaded() { }
    
    public virtual void ClearObjective()
    {
        OnClear?.Invoke();
    }

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
    public string objectiveDescription;
    public string objectiveProgress;
    public bool hasProgression;
    public int indexInList;
    public StoryObjectiveType objectiveType;

    public static StoryObjective GetObjectiveType(StoryObjectiveType type)
    {
        return type switch
        {
            StoryObjectiveType.Destination => CreateInstance<DestinationObjective>(),
            StoryObjectiveType.StoryProgress => CreateInstance<StoryProgressObjective>(),
            StoryObjectiveType.UiUsage => CreateInstance<UiActionObjective>(),
            StoryObjectiveType.Interaction => CreateInstance<InteractionObjective>(),
            StoryObjectiveType.Battle => CreateInstance<BattleObjective>(),
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
    Destination,Interaction,Battle,UiUsage,StoryProgress
}