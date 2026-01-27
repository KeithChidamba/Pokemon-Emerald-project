public abstract class UiActionObjective : StoryObjective
{
    protected override void OnObjectiveLoaded()
    {
        Dialogue_handler.Instance.DisplayObjectiveText(objectiveHeading);
    }
    public override void ClearObjective()
    {
        OverworldState.Instance.ClearAndLoadNextObjective();
    }
}