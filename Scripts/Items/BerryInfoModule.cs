using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Berry", menuName = "berry")]
public class BerryInfoModule : AdditionalInfoModule
{
    [FormerlySerializedAs("herbType")] public Berry berryType;
    public StatusEffect statusEffect;

    public int GetBerryUsage()
    {
        var usageIndex = 0;
        switch (berryType)
        {
            case Berry.FriendshipIncrease:
                usageIndex = 0;
                break;
            case Berry.HpHeal:
                usageIndex = 1;
                break;
            case Berry.StatusHeal:
                usageIndex = 2;
                break;
            case Berry.PpRestore:
                usageIndex = 3;
                break;
            case Berry.ConfusionHeal:
                usageIndex = 4;
                break;
        }
        return usageIndex;
    }
}

public enum Berry
{
    FriendshipIncrease, HpHeal, StatusHeal, PpRestore, ConfusionHeal
}
