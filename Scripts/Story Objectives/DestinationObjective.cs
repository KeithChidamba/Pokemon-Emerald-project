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
    }

    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}
