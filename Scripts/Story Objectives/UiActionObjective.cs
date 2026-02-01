public class UiActionObjective : StoryObjective
{
    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
    }
    protected override void OnObjectiveCleared()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}