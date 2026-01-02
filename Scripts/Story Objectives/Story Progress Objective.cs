using System;
using UnityEngine;
[CreateAssetMenu(fileName = "objective", menuName = "Progress objective")]
[Serializable]
public class StoryProgressObjective : StoryObjective
{
    public int totalObjectiveAmount;
    public int numCompleted;
    public override void LoadSaveData(StoryObjective objectiveData)
    {
        var requiredData = (StoryProgressObjective)GetObjectiveType(objectiveType);
        numCompleted = requiredData.numCompleted;
        totalObjectiveAmount = requiredData.totalObjectiveAmount;
    }
}
