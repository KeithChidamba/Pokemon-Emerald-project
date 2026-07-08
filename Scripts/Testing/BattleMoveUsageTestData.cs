using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/Battle test")]
public class BattleMoveUsageTestData : TestingData
{
    public PlayerData playerTestData;
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