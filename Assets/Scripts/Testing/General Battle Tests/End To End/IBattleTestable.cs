using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBattleTestable
{
    ServiceContainer GetContainer { get; }
}
public static class BattleTestableExtensions
{
    public static void StartBattle(this IBattleTestable target,BattleItemUsageEndToEndTestData testData,EndToEndTest test)
    {
        var testEnemy = Resources.Load<TrainerData>(
            DirectoryHandler.GetDirectory(AssetDirectory.TestAssets) + "Test Enemy");

        testEnemy.TrainerName = testData.testEnemyData.trainerDisplayName;
        testEnemy.PokemonParty = testData.testEnemyData.pokemonParty;
        testEnemy.battleType = testData.testEnemyData.battleType;

        test.testOperationsComplete = target.GetContainer.Resolve<BattleHandler>().AwaitBattleCompletion;
        target.GetContainer.Resolve<BattleHandler>().StartTestBattle(testEnemy);
    }

    public static void EndBattle(this IBattleTestable target)
    {
        target.GetContainer.Resolve<BattleHandler>().EndBattle(BattleEndState.BattleTerminated);
    }
}