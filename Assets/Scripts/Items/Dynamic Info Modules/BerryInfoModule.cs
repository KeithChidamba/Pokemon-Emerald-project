using System;

[Serializable]
public class BerryInfoModule : DynamicAdditionalInfo
{
    public Berry berryType;
}

public enum Berry
{
    FriendshipIncrease = 0, HpHeal = 1, StatusHeal = 2,
    PpRestore = 3, ConfusionHeal = 4, FullStatusHeal = 5
}
