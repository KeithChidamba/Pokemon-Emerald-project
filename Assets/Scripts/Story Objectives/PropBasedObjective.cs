
public class PropBasedObjective : StoryObjective
{
    protected ObjectiveObjectHandler objectiveObjectHandler;
    public void Inject(ObjectiveObjectHandler objectHandler)
    {
        objectiveObjectHandler = objectHandler;
    }
}
