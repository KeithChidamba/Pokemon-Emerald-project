using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildBattleBasedTest : BattleBasedTest
{
    private PokemonPartyHandler _pokemonPartyHandler;
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private DialogueHandler _dialogueHandler;
    
    protected override void EndTest()
    {
        _battleHandler.EndBattle(BattleEndState.BattleTerminated);
        _turnBasedCombatHandler.OnNewTurn -= DetermineTurnUsage;
        _turnBasedCombatHandler.OnTurnEventsCompleted -= LogSuccess;
    }
    protected override IEnumerator HandleBattleState()
    {
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _pokemonPartyHandler = container.Resolve<PokemonPartyHandler>();
        _dialogueHandler = container.Resolve<DialogueHandler>();
        var pokemonOperationsHandler = container.Resolve<PokemonOperations>();
        var wildPokemonAi = container.Resolve<WildPokemonAiHandler>();
        
        var testData = Resources.Load<BattleBasedTestData>(
            DirectoryHandler.GetDirectory(AssetDirectory.Tests) + $"{testName}/Test Data");

        yield return LoadTestData(testData,_pokemonPartyHandler);
        
        _turnBasedCombatHandler.OnNewTurn += DetermineTurnUsage;
        _turnBasedCombatHandler.OnTurnEventsCompleted += LogSuccess;

        var pokemonCreated = false;
        Pokemon createdPokemon = null;
        var wildPokemonData = testData.wildPokemonData.naturalPokemonData;
        
        pokemonOperationsHandler.CreateSpecificPokemon(
            wildPokemon =>
            {
                wildPokemon.moveSet.Clear();
                foreach (var move in wildPokemonData.moveSet)
                {
                    wildPokemon.moveSet.Add(InstanceFactory.CreateMove(move));
                }
                if (wildPokemonData.hasItem) wildPokemon.GiveItem(InstanceFactory.CreateItem(wildPokemonData.heldItem));
                createdPokemon = wildPokemon;
                pokemonCreated = true;
            }
            ,wildPokemonData.pokemon
            ,wildPokemonData.pokemonLevel,
            wildPokemonData.evolutionStageNumber);

        yield return new WaitUntil(() => pokemonCreated);
        
        wildPokemonAi.SetBehavior(BattleAiBehaviorMode.Controlled);
        wildPokemonAi.AssignBehaviorAction(()=>
        {
            //attack player, since its single battle
            var randMove = Utility.RandomRange(0, wildPokemonAi.participant.pokemon.moveSet.Count);
            _battleHandler.UseMove(wildPokemonAi.participant.pokemon.moveSet[randMove]
                ,wildPokemonAi.participant,BattleParticipantKey.Player);
        });
        
        yield return _battleHandler.ProcessWildBattle(createdPokemon,testData.wildPokemonData.biome);
        
        yield return _dialogueHandler.AwaitAllDialogue();      
        
        yield return _battleHandler.AwaitBattleCompletion();

        _pokemonPartyHandler.ClearTestState();
        yield return new WaitForSeconds(0.05f);
    }
}