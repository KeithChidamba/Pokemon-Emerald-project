using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "equipable", menuName = "Item Info Modules/Equipable")]
public class EquipableInfoModule : AdditionalInfoModule
{
    public Equipable equipableItem;
}

public enum Equipable{None,Bike,FishingRod}
