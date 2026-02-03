using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "Herb", menuName = "Item Info Modules/herb")]
public class HerbInfoModule : AdditionalInfoModule
{
    public Herb herbType;
    public StatusEffect statusEffect;
    public ItemType itemType;
    public int GetHerbUsage(Item parentItem)
    {
        var usageIndex = 0;
        switch (herbType)
        {
            case Herb.EnergyPowder:
                parentItem.itemEffect = "50";
                usageIndex = 0;
                break;
            case Herb.EnergyRoot:
                parentItem.itemEffect = "200";
                usageIndex = 0;
                break;
            case Herb.HealPowder:
                statusEffect = StatusEffect.FullHeal;
                usageIndex = 1;
                break;
            case Herb.RevivalHerb:
                itemType = ItemType.MaxRevive;
                usageIndex = 2;
                break;
        }
        return usageIndex;
    }
}

public enum Herb
{
    EnergyPowder, EnergyRoot, HealPowder, RevivalHerb
}
