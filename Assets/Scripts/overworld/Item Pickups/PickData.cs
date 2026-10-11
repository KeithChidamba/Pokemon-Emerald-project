using System;
using UnityEngine;

[Serializable]
public class PickupData
{
    public OverworldPickup pickup;
    [HideInInspector]public string pickupId;
    public bool hasBeenPicked;
    public PickupData(OverworldPickup pickup, bool hasBeenPicked)
    {
        this.pickup = pickup;
        pickupId = pickup.name;
        this.hasBeenPicked = hasBeenPicked;
    }
}