using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "test", menuName = "Tests/Play Test/Battle play test data")]
public class BattlePlayTestData : ScriptableObject
{
    public BattleType battleType;
    public List<AiFlags> trainerAiFlags = new();
    public List<NaturalPokemonCreationData> enemyPartyData = new();
    public List<PokemonTestData> playerPartyData = new();
}
