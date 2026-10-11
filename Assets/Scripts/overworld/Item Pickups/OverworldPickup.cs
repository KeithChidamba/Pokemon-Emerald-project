using System;
using UnityEngine;
[CreateAssetMenu(fileName = "Pickup", menuName = "Overworld/Overworld Item Pickup")]
public class OverworldPickup : ScriptableObject
{
    public Item item;
    public Vector2 itemPosition;
    public int itemQuantity;
}
