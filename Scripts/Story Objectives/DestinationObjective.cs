using System;
using UnityEngine;
[CreateAssetMenu(fileName = "des", menuName = "Objectives/destination objective")]
public class DestinationObjective : StoryObjective
{
    public readonly string destinationTag = "Destination";


    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
    }

    protected override void OnObjectiveCleared()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
