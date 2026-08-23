using System;

[Serializable]
public class BattleHeldItemTypeInfo : DynamicAdditionalInfo
{
    public BattleHeldItem heldItemType;
}

public enum BattleHeldItem
{
    ChoiceBand
}