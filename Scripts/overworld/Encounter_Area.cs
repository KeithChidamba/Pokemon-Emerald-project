using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Encounter", menuName = "enc")]
public class Encounter_Area : ScriptableObject
{
    public enum Biome
    {
        Ocean,UnderWater,OpenField,TallGrass,Mountain,Desert
    };
    public Biome biome;
    public EncounterPokemonData[] availableEncounters;
    public int minimumLevelOfPokemon = 0;
    public int maximumLevelOfPokemon = 0;
    //only for bodies of water
    public int[] pokemonIndexForRodType;
}
