
/// <summary>
/// This class is for restricting item based operations during story objectives
/// </summary>
public class ItemRestriction
{
    private Item itemRestriction;
    private bool restricting;

    public void SetRestriction(Item item)
    {
        itemRestriction = item;
        restricting = true;
    }
    public void RemoveRestriction()
    {
        itemRestriction = null;
        restricting = false;
    }
    public bool ItemRestricted(Item item)
    {
        if (restricting)
        {
            if(item.itemName != itemRestriction.itemName)
            {
                return true;
            }
        }
        return false;
    }
}