
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "dmgModInfo", menuName = "Move Info Modules/dmgModInfo")]

public class DamageModifierInfo : AdditionalInfoModule
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