using System;
using UnityEngine;
[CreateAssetMenu(fileName = "objective", menuName = "Objectives/Progress objective")]
[Serializable]
public class StoryProgressObjective : StoryObjective
{
    public int totalObjectiveAmount;
    public int numCompleted;
    protected override void LoadSaveData(StoryObjective objectiveData)
    {
        var requiredData = (StoryProgressObjective)CreateObjectiveOfType(objectiveType);
        numCompleted = requiredData.numCompleted;
        totalObjectiveAmount = requiredData.totalObjectiveAmount;
    }
}
