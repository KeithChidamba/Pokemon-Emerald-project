using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Berry", menuName = "berry")]
public class BerryInfo : AdditionalItemInfo
{
    public enum Berry
    {
        FriendshipIncrease, HpHeal, StatusHeal
    }
    public Berry herbType;
    public PokemonOperations.StatusEffect statusEffect;

    public int GetBerryUsage()
    {
        var usageIndex = 0;
        switch (herbType)
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
        }
        return usageIndex;
    }
}
