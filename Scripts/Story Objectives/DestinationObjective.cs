using System;
using UnityEngine;
[CreateAssetMenu(fileName = "des", menuName = "destination objective")]
public class DestinationObjective : StoryObjective
{
    public readonly string destinationTag = "Destination";


    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
    }

    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
