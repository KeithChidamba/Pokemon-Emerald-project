using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBattleTestable
{
    ServiceContainer GetContainer { get; }
}
public static class BattleTestableExtensions
{
    public static void StartBattle(this IBattleTestable target,BattleItemUsageEndToEndTestData testData)
    {
        var testEnemy = Resources.Load<TrainerData>(
            DirectoryHandler.GetDirectory(AssetDirectory.TestAssets) + "Test Enemy");

        testEnemy.TrainerName = testData.testEnemyData.trainerDisplayName;
        testEnemy.PokemonParty = testData.testEnemyData.pokemonParty;
        testEnemy.battleType = testData.testEnemyData.battleType;

        target.GetContainer.Resolve<BattleHandler>().StartTestBattle(testEnemy);
    }
}