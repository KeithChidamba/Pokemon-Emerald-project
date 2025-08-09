using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "Herb", menuName = "herb")]
public class HerbInfo : AdditionalItemInfo
{
    public enum Herb
    {
        EnergyPowder, EnergyRoot, HealPowder, RevivalHerb
    }
    public Herb herbType;
    public PokemonOperations.StatusEffect statusEffect;
    public Item_handler.ItemType itemType;
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
                statusEffect = PokemonOperations.StatusEffect.FullHeal;
                usageIndex = 1;
                break;
            case Herb.RevivalHerb:
                itemType = Item_handler.ItemType.MaxRevive;
                usageIndex = 2;
                break;
        }
        return usageIndex;
    }
}
