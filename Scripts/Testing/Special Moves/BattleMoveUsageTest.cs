using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleMoveUsageTest : IntegrationTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    protected ServiceContainer container;
    private PokemonPartyHandler _pokemonPartyHandler;
    private DialogueHandler _dialogueHandler;
    
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
        _battleHandler.EndBattle(BattleEndState.BattleTerminated, null);
        _turnBasedCombatHandler.OnNewTurn -= DetermineTurnUsage;
        _turnBasedCombatHandler.OnTurnsCompleted -= LogSuccess;
    }
    protected virtual void DetermineTurnUsage() { }

    protected IEnumerator HandleBattleState()
    {
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        
        var testData = Resources.Load<BattleMoveUsageTestData>(
            DirectoryHandler.GetDirectory(AssetDirectory.Tests) + $"{testName}/Test Data");

        var testEnemy = Resources.Load<TrainerData>(
            DirectoryHandler.GetDirectory(AssetDirectory.TestAssets) + "Test Enemy");

        testEnemy.TrainerName = testData.testEnemyData.trainerDisplayName;
        testEnemy.PokemonParty = testData.testEnemyData.pokemonParty;
        testEnemy.battleType = testData.testEnemyData.battleType;
        
        yield return LoadTestData(testData);
        
        _turnBasedCombatHandler.OnNewTurn += DetermineTurnUsage;
        _turnBasedCombatHandler.OnTurnsCompleted += LogSuccess;
        
        yield return _battleHandler.SetBattleTypeAndStart(testEnemy);
        
        yield return _dialogueHandler.AwaitAllDialogue();      
        
        yield return _battleHandler.AwaitBattleCompletion();

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

                var invalidNickname = string.IsNullOrEmpty(member.nickName) ||
                                      string.IsNullOrWhiteSpace(member.nickName);
                
                createdPokemon.nickName = invalidNickname? createdPokemon.pokemonName : member.nickName;
                
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