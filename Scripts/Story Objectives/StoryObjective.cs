using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public abstract class StoryObjective : ScriptableObject
{
    public virtual void LoadObjective() { }

    public virtual void ClearObjective() { }

    public virtual void LoadSaveData(StoryObjective objectiveData){ }
    public void FindMainAsset()
    {
        var mainAsset = Resources.Load<StoryObjective>(Save_manager.GetDirectory(AssetDirectory.StoryObjectiveData)+mainAssetName);
        if (mainAsset == null)
        {
            Debug.LogWarning("Story objective Asset: "+mainAssetName+" not found");
            return;
        }
        mainAsset.LoadSaveData(this);
        mainAsset.LoadObjective();
    }
    
    public string mainAssetName;
    public string objectiveHeading;
    public string objectiveDescription;
    public string objectiveProgress;
    public StoryObjectiveType objectiveType;
    public static StoryObjective GetObjectiveType(StoryObjectiveType type)
    {
        return type switch
        {
            StoryObjectiveType.Destination => CreateInstance<DestinationObjective>(),
            StoryObjectiveType.StoryProgress => CreateInstance<StoryProgressObjective>(),
            StoryObjectiveType.UiUsage => CreateInstance<UiActionObjective>(),
            StoryObjectiveType.Interaction => CreateInstance<InteractionObjective>(),
            _ => null
        };
    }
}

[System.Serializable]
public class ObjectiveTypeWrapper
{
    public StoryObjectiveType objectiveType;
}
public enum StoryObjectiveType
{
    Destination,Interaction,Battle,UiUsage,StoryProgress
}