public class UiActionObjective : StoryObjective
{
    protected override void OnObjectiveLoaded()
    {
        var dialogueHandler = serviceContainer.Resolve<Dialogue_handler>(); 
        dialogueHandler.DisplayObjectiveText(objectiveHeading);
        LogicForObjectiveLoad();
    }

    protected virtual void LogicForObjectiveLoad(){}
    
    protected override void OnObjectiveCleared()
    {
        var overworldStateHandler = serviceContainer.Resolve<OverworldState>(); 
        overworldStateHandler.ClearAndLoadNextObjective();
    }
}