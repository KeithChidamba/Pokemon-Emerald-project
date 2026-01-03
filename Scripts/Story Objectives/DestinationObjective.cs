using System;
using UnityEngine;
[CreateAssetMenu(fileName = "des", menuName = "destination objective")]
public class DestinationObjective : StoryObjective
{
    public readonly string destination = "Destination";
    public event Action OnLoad;

    public override void LoadObjective()
    {
        OnLoad?.Invoke();
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
    }

    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
        Dialogue_handler.Instance.RemoveObjectiveText();
    }
}
