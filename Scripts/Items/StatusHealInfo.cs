
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "StatusHeal", menuName = "statusHeal")]
public class StatusHealInfo : AdditionalItemInfo
{
    public PokemonOperations.StatusEffect statusEffect;
}
