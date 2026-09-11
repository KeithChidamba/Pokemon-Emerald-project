using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Berry", menuName = "Item Info Modules/berry")]
public class BerryInfoModule : AdditionalInfoModule
{
    [FormerlySerializedAs("herbType")] public Berry berryType;
    public StatusEffect statusEffect;
}

public enum Berry
{
    FriendshipIncrease = 0, HpHeal = 1, StatusHeal = 2,
    PpRestore = 3, ConfusionHeal = 4, FullStatusHeal = 5
}
