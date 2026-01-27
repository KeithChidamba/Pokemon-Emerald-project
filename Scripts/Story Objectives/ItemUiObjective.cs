using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUiObjective : UiActionObjective
{
    public Item itemForObjective;
    protected void CheckForItemObjectiveClear(Item item)
    {
        if (itemForObjective.itemName == item.itemName)
        {
            ClearObjective();
        }
    }
}
