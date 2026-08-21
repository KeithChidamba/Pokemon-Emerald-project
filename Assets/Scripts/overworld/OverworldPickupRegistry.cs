using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "registry", menuName = "Overworld/Overworld Item Registry")]
public class OverworldPickupRegistry : ScriptableObject
{
    public List<PickupData> overworldPickups = new ();
    private Dictionary<Vector2,PickupData> _overworldPickupPositions = new();
    private Dictionary<int,GameObject> _overworldPickupObjects = new();
    public void LoadLookup(GameObject overworldPickupPrefab, Transform overworldPickupParent,OverworldState state)
    {
        foreach (var currentPickupData in overworldPickups)
        {
            if(currentPickupData.hasBeenPicked)continue;
            _overworldPickupPositions.Add(currentPickupData.pickup.itemPosition,currentPickupData);
            var newPickupObject = Instantiate(overworldPickupPrefab,currentPickupData.pickup.itemPosition,overworldPickupPrefab.transform.rotation, overworldPickupParent);
            newPickupObject.SetActive(true);
            _overworldPickupObjects.Add(currentPickupData.pickupId,newPickupObject);
            state.AlertPickupItemCreation(currentPickupData);
        }
    }

    public Item GetItemPickup(Vector2 interactionPosition)
    {
        if (_overworldPickupPositions.TryGetValue(interactionPosition, out var pickupData))
        {
            pickupData.hasBeenPicked = true;
            var itemCopy = InstanceFactory.CreateItem(pickupData.pickup.item); 
            itemCopy.quantity = pickupData.pickup.itemQuantity;
            return itemCopy;
        }
        return null;
    }
    public GameObject FindPickupObject(int id)
    {
        return _overworldPickupObjects[id];
    }
}
[Serializable]
public class PickupData
{
    public OverworldPickup pickup;
    public bool hasBeenPicked;
    private OverworldPickupRegistry _registry;
    [HideInInspector]public int pickupId;
    
    public PickupData(OverworldPickup pickup, bool hasBeenPicked,OverworldPickupRegistry registry)
    {
        this.pickup = pickup;
        this.hasBeenPicked = hasBeenPicked;
        pickupId = Utility.Random16Bit();
        _registry = registry;
    }
    public GameObject GetPickupObject()
    {
        return _registry.FindPickupObject(pickupId);
    }
}