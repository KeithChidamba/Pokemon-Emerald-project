using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "Herb", menuName = "Item Info Modules/herb")]
public class HerbInfoModule : AdditionalInfoModule
{
    public Herb herbType;
    public StatusEffect statusEffect;
    public ItemType itemType;
    public int GetHerbUsage()
    {
        switch (herbType)
        {
            case Herb.EnergyPowder:
            case Herb.EnergyRoot:
                return 0;
            case Herb.HealPowder:
                statusEffect = StatusEffect.FullHeal;
                return 1;
            case Herb.RevivalHerb:
                itemType = ItemType.MaxRevive;
                return 2;
            default: return 0;
        }
    }
}

public enum Herb
{
    EnergyPowder, EnergyRoot, HealPowder, RevivalHerb
}
