using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "equipable", menuName = "Equipable")]
public class EquipableInfoModule : AdditionalInfoModule
{
    public Equipable equipableItem;
}

public enum Equipable{None,Bike,FishingRod}
