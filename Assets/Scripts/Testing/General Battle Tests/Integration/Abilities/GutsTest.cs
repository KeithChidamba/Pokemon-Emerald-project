using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class GutsTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Guts Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(ForceSpecificMove);
    }
    private void ForceSpecificMove()
    {        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseMove);
        _sequencer.UseMove();
        return;
        void UseMove()
        {
            //toxic
            enemy.pokemon.moveSet[0].priority = 100;
            enemy.pokemon.moveSet[0].statusChance = 100;
            enemy.pokemon.moveSet[0].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[0], enemy, BattleParticipantKey.Player);
        }
    }
   
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must be badly poisoned",
                ()=>player.pokemon.statusEffect == StatusEffect.BadlyPoison),
            new("Player must have status and attack buff from guts",
                ()=>player.pokemon.attack > player.statData.attack),
        });
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);

        testingHandler.LogMessage($"Status effect on player: { player.pokemon.statusEffect}",TestLogType.Information);
        testingHandler.LogMessage($"Attack on player: { player.pokemon.attack}/{player.statData.attack}",TestLogType.Information);
        
        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
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

