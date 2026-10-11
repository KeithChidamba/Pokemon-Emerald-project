using System;
using System.Collections.Generic;

[Serializable]
public class DamageModifierInfo : DynamicAdditionalInfo
{
    public string damageChangeMessage;
    public List<DamageModifierForType> damageModifiers = new();
    public DamageModifierSource modifierSource;
}
public enum DamageModifierSource{WaterSport,MudSport,Rain,Sunlight}

[Serializable]
public struct DamageModifierForType
{
    public PokemonType typeAffected;
    public float damageFactor;

    public DamageModifierForType(PokemonType typeAffected, float damageFactor)
    {
        this.typeAffected = typeAffected;
        this.damageFactor = damageFactor;
    }
}