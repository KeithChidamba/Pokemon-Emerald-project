using System;
using UnityEngine;
[CreateAssetMenu(fileName = "Pickup", menuName = "Overworld/Overworld Item Pickup")]
public class OverworldPickup : ScriptableObject
{
    public Item item;
    public string itemAssetName;
    public Vector2 itemPosition;
    public bool hasBeenPicked;
}
