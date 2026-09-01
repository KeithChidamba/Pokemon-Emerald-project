using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class SilverwindSwapTest : BattleBasedTest
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
        testName = "Silverwind Swap Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(EnsureEnemyFaintAndSilverwindBuffs);
    }

    private void EnsureEnemyFaintAndSilverwindBuffs()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].statusChance = 100;
        player.pokemon.moveSet[0].priority = 100;
        enemy.pokemon.hp = 1;
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase("Silverwind buffs should be applied after enemy sends out partner",
            () => player.pokemon.statModifiers.Count > 0);

        _battleHandler.SetBattleStyle((int)BattleHandler.BattlesStyle.Set);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
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

