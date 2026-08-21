
public class PropBasedObjective : StoryObjective
{
    protected ObjectiveObjectHandler objectiveObjectHandler;
    public bool requiresPickupItems;

    public void Inject(ObjectiveObjectHandler objectHandler,ServiceContainer container)
    {
        objectiveObjectHandler = objectHandler;
        //for pre-objective load logic
        serviceContainer = container;
    }
    public virtual void ReceivePickupObjects(PickupData data) { }
}
