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
        enemy.pokemon.moveSet[0].isSureHit = true;
        _sequencer.UseMove();
    }
    private void SetupEnemyStruggle()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Natural);
        enemy.pokemon.moveSet[0].powerpoints = 0;
        enemy.pokemon.moveSet[1].powerpoints = 0;
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        //tailwhip (to keep up test case)
        player.pokemon.moveSet[1].powerpoints = 1;
        _sequencer.UseMove(1);
    }
    private void SimulateAttackToUseStruggle()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].powerpoints = 0;
        player.pokemon.moveSet[1].powerpoints = 0;
        
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        
        //The current UI is battle options, so simulate player selecting "FIGHT"
        _battleHandler.LoadMoveInputAndText();
        return;
        void UseSpecificMove()
        {
            //tailwhip (to keep up test case)
            enemy.pokemon.moveSet[1].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[1], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must be hurt",
                ()=> player.pokemon.hp < player.pokemon.maxHp),
            new("Enemy must be hurt",
                ()=> enemy.pokemon.hp < enemy.pokemon.maxHp),
        });
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must hurt enemy",
                ()=> enemy.pokemon.hp < enemy.pokemon.maxHp),
            new("Player must be hurt by recoil",
                ()=> player.pokemon.hp < player.pokemon.maxHp)
        });
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Enemy must hurt player",
                ()=> player.pokemon.hp < player.pokemon.maxHp),
            new("Enemy must be hurt by recoil",
                ()=> enemy.pokemon.hp < enemy.pokemon.maxHp)
        });
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);
        testingHandler.LogMessage($"Health of player: {player.pokemon.hp}" +
                                  $"/{player.pokemon.maxHp}",TestLogType.Health);
        
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            //for struggle test cases
            enemy.pokemon.hp = enemy.pokemon.maxHp;
            player.pokemon.hp = player.pokemon.maxHp;
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

