using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class StruggleTest : BattleBasedTest
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
        testName = "Struggle Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //both attack normally with tackle
        _sequencer.AddAction(AttackNormally);
        //player will use struggle -> enemy use tailwhip
        _sequencer.AddAction(SimulateAttackToUseStruggle);
        //enemy will use struggle
        _sequencer.AddAction(SetupEnemyStruggle);
    }

    private void AttackNormally()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.Pokemon.moveSet[0].isSureHit = true;
        _sequencer.UseMove();
    }
    private void SetupEnemyStruggle()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);
        enemy.Pokemon.moveSet[0].powerpoints = 0;
        enemy.Pokemon.moveSet[1].powerpoints = 0;
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //tailwhip (to keep up test case)
        player.Pokemon.moveSet[1].powerpoints = 1;
        _sequencer.UseMove(1);
    }
    private void SimulateAttackToUseStruggle()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.Pokemon.moveSet[0].powerpoints = 0;
        player.Pokemon.moveSet[1].powerpoints = 0;
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        
        //The current UI is battle options, so simulate player selecting "FIGHT"
        _battleHandler.LoadMoveInputAndText();
        return;
        void UseSpecificMove()
        {
            //tailwhip (to keep up test case)
            enemy.Pokemon.moveSet[1].isSureHit = true;
            _battleHandler.UseMove(enemy.Pokemon.moveSet[1], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must be hurt",
                ()=> player.Pokemon.hp < player.Pokemon.maxHp),
            new("Enemy must be hurt",
                ()=> enemy.Pokemon.hp < enemy.Pokemon.maxHp),
        });
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must hurt enemy",
                ()=> enemy.Pokemon.hp < enemy.Pokemon.maxHp),
            new("Player must be hurt by recoil",
                ()=> player.Pokemon.hp < player.Pokemon.maxHp)
        });
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Enemy must hurt player",
                ()=> player.Pokemon.hp < player.Pokemon.maxHp),
            new("Enemy must be hurt by recoil",
                ()=> enemy.Pokemon.hp < enemy.Pokemon.maxHp)
        });
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.Pokemon.hp}" +
                                  $"/{enemy.Pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of player: {player.Pokemon.hp}" +
                                  $"/{player.Pokemon.maxHp}",TestLogType.Health);
        
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            //for struggle test cases
            enemy.Pokemon.hp = enemy.Pokemon.maxHp;
            player.Pokemon.hp = player.Pokemon.maxHp;
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

