using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMoveUsageTest : IntegrationTest
{
    protected Battle_handler battleHandler;
    protected Turn_Based_Combat turnBasedCombatHandler;
    protected ServiceContainer container;
    private Pokemon_party _pokemonPartyHandler;
    private Dialogue_handler _dialogueHandler;
    
    protected TestCompletionCondition testExitCondition;

    protected enum TestCompletionCondition
    {
        EndAfterTurns,EndManually
    };
    protected virtual void DetermineSuccess() { }

    private void LogSuccess()
    {
        DetermineSuccess();
        if (testExitCondition == TestCompletionCondition.EndAfterTurns)
        {
            EndTest();
        }
    }

    protected void EndTest()
    {
        battleHandler.EndBattle(BattleEndState.BattleTerminated, null);
        turnBasedCombatHandler.OnNewTurn -= DetermineMoveUsage;
        turnBasedCombatHandler.OnTurnsCompleted -= LogSuccess;
    }
    protected virtual void DetermineMoveUsage() { }

    protected IEnumerator HandleBattleState()
    {
        battleHandler = container.Resolve<Battle_handler>();
        turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        
        var testData = Resources.Load<BattleMoveUsageTestData>(
            DirectoryHandler.GetDirectory(AssetDirectory.Tests) + $"{testName}/Test Data");

        var testEnemy = Resources.Load<TrainerData>(
            DirectoryHandler.GetDirectory(AssetDirectory.TestAssets) + "Test Enemy");

        testEnemy.TrainerName = testData.testEnemyData.trainerDisplayName;
        testEnemy.PokemonParty = testData.testEnemyData.pokemonParty;
        testEnemy.battleType = testData.testEnemyData.battleType;
        
        yield return LoadTestData(testData);
        
        turnBasedCombatHandler.OnNewTurn += DetermineMoveUsage;
        turnBasedCombatHandler.OnTurnsCompleted += LogSuccess;
        
        yield return battleHandler.SetBattleTypeAndStart(testEnemy);
        
        yield return _dialogueHandler.AwaitAllDialogue();      
        
        yield return battleHandler.AwaitBattleCompletion();

        _pokemonPartyHandler.ClearTestState();
        yield return new WaitForSeconds(0.05f);
    }
    private IEnumerator LoadTestData(BattleMoveUsageTestData testData)
    {
        var pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        
        foreach (var member in testData.pokemonPartyData)
        {
            yield return pokemonOperationsHandler.HandlePokemonCreation(CreateMember
                ,member.naturalPokemonData.pokemon
                ,member.naturalPokemonData.pokemonLevel
                ,member.naturalPokemonData.evolutionStageNumber);
            
            void CreateMember(Pokemon createdPokemon)
            {
                createdPokemon.nature = member.specificNature;
                createdPokemon.gender = member.specificGender;
                createdPokemon.moveSet.Clear();
                foreach (var move in member.naturalPokemonData.moveSet)
                {
                    createdPokemon.moveSet.Add(InstanceFactory.CreateMove(move));
                }
                if(member.naturalPokemonData.hasItem)
                {
                    createdPokemon.GiveItem(InstanceFactory.CreateItem(member.naturalPokemonData.heldItem));
                }
                _pokemonPartyHandler.AddTestMember(createdPokemon);
            }
        }
        yield return new WaitForSeconds(1f);
    }
}