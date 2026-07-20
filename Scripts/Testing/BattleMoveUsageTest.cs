using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMoveUsageTest : IntegrationTest
{
    protected Battle_handler battleHandler;
    protected Turn_Based_Combat turnBasedCombatHandler;
    protected ServiceContainer container;
    private Pokemon_party _pokemonPartyHandler;
    protected virtual void DetermineSuccess() { }

    private void LogSuccess()
    {
        DetermineSuccess();
        battleHandler.EndBattle(BattleEndState.BattleTerminated, null);
        turnBasedCombatHandler.OnNewTurn -= DetermineMoveUsage;
    }

    protected virtual void DetermineMoveUsage() { }

    protected IEnumerator HandleBattleState()
    {
        battleHandler = container.Resolve<Battle_handler>();
        turnBasedCombatHandler = container.Resolve<Turn_Based_Combat>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        
        var testData = Resources.Load<BattleMoveUsageTestData>(
            DirectoryHandler.GetDirectory(AssetDirectory.Tests) + $"{testName}/Test Data");
       
        yield return LoadTestData(testData);
        
        turnBasedCombatHandler.OnNewTurn += DetermineMoveUsage;
        turnBasedCombatHandler.OnTurnsCompleted += LogSuccess;
        
        yield return battleHandler.SetBattleTypeAndStart(testData.testEnemy);
        
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