using System;
using UnityEngine;
[CreateAssetMenu(fileName = "des", menuName = "Objectives/destination objective")]
public class DestinationObjective : StoryObjective
{
    public readonly string destinationTag = "Destination";
    private OverworldState _overworldStateHandler;

    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        _overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
    }

    protected override void OnObjectiveCleared()
    {
        _overworldStateHandler.ClearAndLoadNextObjective();
    }
}
