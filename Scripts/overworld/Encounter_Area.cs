using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Encounter", menuName = "enc")]
public class Encounter_Area : ScriptableObject
{
    [FormerlySerializedAs("Biome_name")] public string biomeName;
    [FormerlySerializedAs("Location")] public string location;
    public EncounterPokemonData[] availableEncounters;
    public int minimumLevelOfPokemon = 0;
    public int maximumLevelOfPokemon = 0;
    //only for bodies of water
    public int[] pokemonIndexForRodType;
}
