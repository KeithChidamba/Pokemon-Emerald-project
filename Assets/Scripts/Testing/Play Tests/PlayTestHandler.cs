
using System.Collections;
using UnityEngine;

public class PlayTestHandler : MonoBehaviour
{
    public TrainerData testEnemy;
    public BattlePlayTestData currentPlayTest;

    public IEnumerator BeginTest(ServiceContainer container)
    {
        yield return TestingUtilities.LoadPokemonPartyTestData(currentPlayTest.playerPartyData
            ,container.Resolve<PokemonPartyHandler>()
            ,container.Resolve<PokemonOperations>());
        
        testEnemy.PokemonParty.Clear();
        foreach (var member in currentPlayTest.enemyPartyData)
        {
            var newData = ScriptableObject.CreateInstance<TrainerPokemonData>();
            newData.data = member;
            testEnemy.PokemonParty.Add(newData);
        }
        testEnemy.battleType = currentPlayTest.battleType;
        testEnemy.trainerAiFlags.Clear();
        testEnemy.trainerAiFlags.AddRange(currentPlayTest.trainerAiFlags);
        container.Resolve<BattleHandler>().StartBattle(testEnemy);
    }
}
