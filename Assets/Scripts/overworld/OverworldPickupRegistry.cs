using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
[CreateAssetMenu(fileName = "registry", menuName = "Overworld/Overworld Item Registry")]
public class OverworldPickupRegistry : ScriptableObject
{
    public List<PickupData> overworldPickups = new ();
    private Dictionary<Vector2,PickupData> _overworldPickupPositions = new();
    private GameObject _overworldPickupPrefab;
    private Transform _overworldPickupParent;
    private readonly Dictionary<Vector2, GameObject> _spawned = new();
    
    public void LoadLookup(GameObject overworldPickupPrefab, Transform overworldPickupParent)
    {
        _overworldPickupPositions.Clear();
        _overworldPickupPrefab = overworldPickupPrefab;
        _overworldPickupParent = overworldPickupParent;
       
        foreach (var currentPickupData in overworldPickups)
        {
            if(currentPickupData.hasBeenPicked)continue;
            SetupPickup(currentPickupData);
        }
    }
    private void SetupPickup(PickupData currentPickupData)
    {
        var pos = currentPickupData.pickup.itemPosition;
        if (!_overworldPickupPositions.TryAdd(pos, currentPickupData))
        {
            Debug.LogError($"Duplicate pickup position {pos} in registry");
            return;
        }
        var newPickupObject = Instantiate(_overworldPickupPrefab, pos, _overworldPickupPrefab.transform.rotation, _overworldPickupParent);
        newPickupObject.SetActive(true);
        _spawned[pos] = newPickupObject;
    }
    public void AddToLookUp(PickupData newPickupData)
    {
        overworldPickups.Add(newPickupData);
        SetupPickup(newPickupData);
    }
    
    /// <summary>
    /// Item position is treated as a unique identifier, because 2 items should never overlap
    /// </summary>
    [CanBeNull]
    public Item TakeItemPickup(Vector2 interactionPosition)
    {
        if (_overworldPickupPositions.TryGetValue(interactionPosition, out var pickupData))
        {
            if (pickupData.pickup.item is null) throw new Exception($"Item Pickup registry has null item at [{pickupData.pickup.itemPosition}]");
            
            var itemCopy = InstanceFactory.CreateItem(pickupData.pickup.item);
            pickupData.hasBeenPicked = true;
            itemCopy.quantity = pickupData.pickup.itemQuantity;
            _overworldPickupPositions.Remove(interactionPosition);
            if (_spawned.Remove(interactionPosition, out var pickupObject))
            {
                Destroy(pickupObject);
            }
            return itemCopy;
        }
        return null;
    }
}
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