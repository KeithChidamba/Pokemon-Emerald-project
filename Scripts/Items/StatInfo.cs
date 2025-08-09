using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu(fileName = "stat", menuName = "statInfo")]

public class StatInfo : AdditionalItemInfo
{
    public PokemonOperations.Stat statName;
}
