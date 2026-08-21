using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/Battle based test data")]
public class BattleBasedTestData : ScriptableObject
{
    public List<PokemonTestData> pokemonPartyData = new();
    public TestTrainerData testEnemyData;
    public WildPokemonTestData wildPokemonData;
}

[Serializable]
public struct WildPokemonTestData
{
    public NaturalPokemonCreationData naturalPokemonData;
    public Biome biome;
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
    public Ability specificAbility;
    public string nickName;
}