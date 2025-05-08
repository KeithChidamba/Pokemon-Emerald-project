using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Pokemon_ablty", menuName = "pkm_a")]
public class Ability : ScriptableObject
{
    public string abilityName;
    [FormerlySerializedAs("ability_description")] public string abilityDescription = "";
}
