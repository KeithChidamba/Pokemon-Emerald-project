using System;

[Serializable]
public class OverworldUsageItem : DynamicAdditionalInfo
{
    public SpecialOverworldItem specialItem;
}

public enum SpecialOverworldItem
{
    EscapeRope
}