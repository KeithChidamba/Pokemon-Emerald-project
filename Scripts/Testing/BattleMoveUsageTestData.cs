using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/Battle move test")]
public class BattleMoveUsageTestData : ScriptableObject
{
    public List<PokemonTestData> pokemonPartyData = new();
    public TestTrainerData testEnemyData;
}

[Serializable]
public struct TestTrainerData
{
    public string trainerDisplayName;
    public BattleType battleType; 
    public List<TrainerPokemonData> pokemonParty;
}
[Serializable]
public struct PokemonTestData
{
    public NaturalPokemonCreationData naturalPokemonData;
    public Nature specificNature;
    public Gender specificGender;
}