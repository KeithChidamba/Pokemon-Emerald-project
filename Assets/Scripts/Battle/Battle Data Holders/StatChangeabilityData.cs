using System;

[Serializable]
public class StatChangeabilityData
{
    public StatChangeability changeability;
    public int effectDuration;

    public StatChangeabilityData(StatChangeability changeability, int effectDuration)
    {
        this.changeability = changeability;
        this.effectDuration = effectDuration;
    }
}

public enum StatChangeability{ImmuneToIncrease,ImmuneToDecrease}
