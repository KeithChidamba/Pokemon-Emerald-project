using System;
[Serializable]
public class ExpModifierInfo : DynamicAdditionalInfo
{
    public ExpModifier modifier;
    public float modifierFactor;
}

public enum ExpModifier
{
    ExpShare,ExpGainBonus
}