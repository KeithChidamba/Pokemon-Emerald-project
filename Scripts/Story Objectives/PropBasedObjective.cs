
public class PropBasedObjective : StoryObjective
{
    protected ObjectiveObjectHandler objectiveObjectHandler;
    public bool requiresPickupItems;

    public void Inject(ObjectiveObjectHandler objectHandler)
    {
        objectiveObjectHandler = objectHandler;
    }
    public virtual void ReceivePickupObjects(PickupData data) { }
}
