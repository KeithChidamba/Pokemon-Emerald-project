using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "equipable", menuName = "Equipable")]
public class EquipableInfoModule : AdditionalInfoModule
{
    public enum Equipable{None,Bike,FishingRod}

    public Equipable equipableItem;
}
