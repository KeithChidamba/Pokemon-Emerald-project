using System;

[Serializable]
public class EquipableItemInfo : DynamicAdditionalInfo
{
    public Equipable equipableItem;
}
public enum Equipable{None,Bike,FishingRod}