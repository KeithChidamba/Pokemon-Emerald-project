using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Encounter", menuName = "Overworld/Encounter Area")]
public class Encounter_Area : ScriptableObject
{
    public Biome biome;
    public EncounterPokemonData[] availableEncounters;
    public int minimumLevelOfPokemon;
    public int maximumLevelOfPokemon;
    //only for bodies of water
    public int[] pokemonIndexForRodType;
}

public enum Biome
{
    Ocean,UnderWater,OpenField,TallGrass,Mountain,Desert,Beach,InDoors
};
