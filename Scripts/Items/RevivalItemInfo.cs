using UnityEngine;

[CreateAssetMenu(fileName = "Rev", menuName = "Item Info Modules/Revival item Info")]
public class RevivalItemInfo : AdditionalInfoModule
{
    public RevivalItemType reviveType;
}

public enum RevivalItemType
{
    HalfHealth,
    FullHealth
}