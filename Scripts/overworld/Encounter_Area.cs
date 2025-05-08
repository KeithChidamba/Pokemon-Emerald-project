using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Encounter", menuName = "enc")]
public class Encounter_Area : ScriptableObject
{
    [FormerlySerializedAs("Biome_name")] public string biomeName;
    [FormerlySerializedAs("Location")] public string location;
    [FormerlySerializedAs("Pokemon")] public string[] availablePokemon;
    [FormerlySerializedAs("min_lv")] public int minimumLevelOfPokemon = 0;
    [FormerlySerializedAs("max_lv")] public int maximumLevelOfPokemon = 0;
}
