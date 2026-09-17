
using System.Collections;
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
    public static void StartWildBattle(this IBattleTestable target,BattleItemUsageEndToEndTestData testData,EndToEndTest test)
    {
        var wildPokemonAi = target.GetContainer.Resolve<WildPokemonAiHandler>();
        var wildPokemonData = testData.wildPokemonData.naturalPokemonData;
        var battleHandler = target.GetContainer.Resolve<BattleHandler>();
        var pokemonOperationsHandler = target.GetContainer.Resolve<PokemonOperations>();
        
        pokemonOperationsHandler.CreateSpecificPokemon(
            wildPokemon =>
            {
                wildPokemon.moveSet.Clear();
                foreach (var move in wildPokemonData.moveSet)
                {
                    wildPokemon.moveSet.Add(InstanceFactory.CreateMove(move));
                }
                if (wildPokemonData.hasItem) wildPokemon.GiveItem(InstanceFactory.CreateItem(wildPokemonData.heldItem));
                
                wildPokemonAi.SetBehavior(BattleAiBehaviorMode.Controlled);
                wildPokemonAi.AssignBehaviorAction(()=>
                {
                    //attack player, prevent running away
                    var randMove = Utility.RandomRange(0, wildPokemonAi.participant.pokemon.moveSet.Count);
                    battleHandler.UseMove(wildPokemonAi.participant.pokemon.moveSet[randMove]
                        ,wildPokemonAi.participant,BattleParticipantKey.Player);
                });
        
                test.testOperationsComplete = battleHandler.AwaitBattleCompletion;
                battleHandler.StartWildBattle(wildPokemon,testData.wildPokemonData.biome);
            }
            ,wildPokemonData.pokemon
            ,wildPokemonData.pokemonLevel,
            wildPokemonData.evolutionStageNumber);
    }
    public static void EndBattle(this IBattleTestable target)
    {
        target.GetContainer.Resolve<BattleHandler>().EndBattle(BattleEndState.BattleTerminated);
    }
}