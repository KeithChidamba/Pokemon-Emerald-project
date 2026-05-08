using System;
using UnityEngine;
[CreateAssetMenu(fileName = "des", menuName = "Objectives/destination objective")]
public class DestinationObjective : StoryObjective
{
    private OverworldState _overworldStateHandler;
    public Vector3 destinationPosition;
    
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
