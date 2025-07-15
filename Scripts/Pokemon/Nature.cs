using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Nature", menuName = "ntr")]
public class Nature : ScriptableObject
{
    public string natureName;
    public PokemonOperations.Stat statToIncrease;
    public PokemonOperations.Stat statToDecrease;
    [FormerlySerializedAs("PValue")] public int requiredNatureValue;
}
