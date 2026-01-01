using UnityEngine;
[System.Serializable]
public abstract class StoryObjective : ScriptableObject
{
    public abstract void LoadObjective();
    public abstract void ClearObjective();
    public string objectiveMessage;
    public string objectiveProgress;
}
