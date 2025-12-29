
using UnityEngine;

[CreateAssetMenu(fileName = "dmgModInfo", menuName = "dmgModInfo")]

public class DamageModifierInfo : AdditionalInfoModule
{
    public string damageChangeMessage;
    public Types typeAffected;
    public float damageModifier;
}