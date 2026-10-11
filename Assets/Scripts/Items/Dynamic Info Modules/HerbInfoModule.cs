using System;

[Serializable]
public class HerbInfoModule : DynamicAdditionalInfo
{
    public Herb herbType;
    public int GetHerbUsage()
    {
        switch (herbType)
        {
            case Herb.EnergyPowder:
            case Herb.EnergyRoot:
                return 0;
            case Herb.HealPowder:
                return 1;
            case Herb.RevivalHerb:
                return 2;
            default: return 0;
        }
    }
}

public enum Herb
{
    EnergyPowder, EnergyRoot, HealPowder, RevivalHerb
}
