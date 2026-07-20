using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/Battle move test")]
public class BattleMoveUsageTestData : ScriptableObject
{
    public List<PokemonTestData> pokemonPartyData = new();
    public TrainerData testEnemy;
}
[Serializable]
public struct PokemonTestData
{
    public NaturalPokemonCreationData naturalPokemonData;
    public Nature specificNature;
    public Gender specificGender;
}