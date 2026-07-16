using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMoveUsageTest : IntegrationTest
{
    public override void Inject(ServiceContainer container, TestingData data)
    {
        testData = (BattleMoveUsageTestData)data;
        _dialogueHandler = container.Resolve<Dialogue_handler>();
        _battleHandler = container.Resolve<Battle_handler>();
        _pokemonPartyHandler = container.Resolve<Pokemon_party>();
        _pokemonOperationsHandler = container.Resolve<PokemonOperations>();
    }
    
    private BattleMoveUsageTestData testData;
    private Dialogue_handler _dialogueHandler;
    private Battle_handler _battleHandler;
    private Pokemon_party _pokemonPartyHandler;
    private PokemonOperations _pokemonOperationsHandler;
    
    public override IEnumerator BeginTest()
    {
        foreach (var member in testData.pokemonPartyData)
        {
            yield return _pokemonOperationsHandler.HandlePokemonCreation(CreateMember
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
        _battleHandler.SetBattleTypeAndStart(testData.testEnemy);
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => _battleHandler.battleOver);
        yield return null;
        onTestResult.Invoke();
    }
}