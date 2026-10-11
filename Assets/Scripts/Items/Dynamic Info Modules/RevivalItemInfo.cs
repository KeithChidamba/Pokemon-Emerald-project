using System;

[Serializable]
public class RevivalItemInfo : DynamicAdditionalInfo
{
    public RevivalItemType reviveType;
}

public enum RevivalItemType
{
    HalfHealth,
    FullHealth
}