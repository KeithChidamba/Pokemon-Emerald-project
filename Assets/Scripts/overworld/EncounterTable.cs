
using System;
using UnityEngine;

public class EncounterTable : ScriptableObject
{
    public Biome biome;
}

public enum Biome
{
    Pond,
    Ocean,
    UnderWater,
    OpenField,
    TallGrass,
    Mountain,
    Desert,
    Beach,
    InDoors
};
[Serializable]
public struct EncounterTableData{
    public EncounterPokemonData[] availableEncounters;
    public int minimumLevelOfPokemon;
    public int maximumLevelOfPokemon;
}
