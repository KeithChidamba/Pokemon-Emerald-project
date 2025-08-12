using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "equipable", menuName = "Equipable")]
public class EquipableItemInfo : AdditionalItemInfo
{
    public enum Equipable{None,Bike,FishingRod}

    public Equipable equipableItem;
}
