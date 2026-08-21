using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class WhirlwindTrainerBattleTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    private long _currentEnemyID;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Whirlwind Trainer Battle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
         //whirlwind[should fail] -> tailwhip
         _sequencer.AddAction(UseWhirlWind);
         //tackle[should faint enemy] -> tailwhip
         _sequencer.AddAction(ForceEnemyFaint);
         //whirlwind[should work] -> tailwhip
         _sequencer.AddAction(UseWhirlWind);
         //tackle[should faint enemy] -> tailwhip
         _sequencer.AddAction(ForceEnemyFaint);
         //whirlwind[should fail -  no more available swaps] -> tailwhip
         _sequencer.AddAction(UseWhirlWind);
    }

    private void UseWhirlWind()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        _currentEnemyID = enemy.pokemon.pokemonID;
        _sequencer.UseMove();
    }
    private void ForceEnemyFaint()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        //ensure faint
        enemy.pokemon.hp = 2;
        //tackle
        _currentEnemyID = enemy.pokemon.pokemonID;
        _sequencer.UseMove(1);
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase("Whirlwind should fail due to level gap",
            () => player.pokemon.currentLevel < enemy.pokemon.currentLevel);
        
        _testCaseHandler.AddTestCase("Enemy was fainted",
            () => enemy.pokemonTrainerAI.GetLivingPokemonCount() < enemy.pokemonTrainerAI.TrainerParty.Count);
        
        _testCaseHandler.AddTestCase("Whirlwind should succeed",
            () => _currentEnemyID != enemy.pokemon.pokemonID);
        
        _testCaseHandler.AddTestCase("Enemy was fainted",
            () => enemy.pokemonTrainerAI.GetLivingPokemonCount() < enemy.pokemonTrainerAI.TrainerParty.Count-1);

        _testCaseHandler.AddTestCase("Whirlwind should fail",
            () => _currentEnemyID == enemy.pokemon.pokemonID);
        
        //for testing purposes, disable the switch style
        _battleHandler.SetBattleStyle((int)BattleHandler.BattlesStyle.Set);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            if (_sequencer.SequenceComplete())
            {
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            //add extra logic here
            EndTest(false);
        }
    }
    protected override void DetermineTurnUsage()
    {
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        if (currentParticipant.participantKey is BattleParticipantKey.Enemy or BattleParticipantKey.EnemyPartner)
        {
            return;
        }
        _sequencer.CallNextAction();
    }
}

