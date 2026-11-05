
using UnityEngine;

[System.Serializable]
public struct EncounterPokemonData
{
    public Pokemon pokemon;
    public int encounterChance;
    public int evolutionFormNumber;
    [Range(0, 1)] public int splitEvolutionIndex;
}
