using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Trainer Pkm Data", menuName = "Battle/Trainer/trainer pkm data")]
public class TrainerPokemonData : ScriptableObject
{
    public NaturalPokemonCreationData data;
}
[Serializable]
public struct NaturalPokemonCreationData
{
    public Pokemon pokemon;
    public int pokemonLevel;
    public int evolutionStageNumber;
    public bool hasItem;
    public Item heldItem;
    public List<Move> moveSet;
}