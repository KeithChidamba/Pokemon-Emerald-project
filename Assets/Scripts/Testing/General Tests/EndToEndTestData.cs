using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/End To End/End To End test data")]
public class EndToEndTestData : ScriptableObject
{
    public List<PokemonTestData> pokemonPartyData = new();
    public string testDescription;
}
