using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class StaticTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Static Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(AttackFirst);
        //Static has a random chance of activation, so we 
        //simulate it as a guarantee
        _moveUsageHandler.OnMoveHit += SimulateStatic;
    }

    private void SimulateStatic(BattleParticipant attacker,BattleParticipant victim,Move moveUsed,float finalDamage)
    {
        if (attacker.participantKey == BattleParticipantKey.Player)
        {
            victim.abilityHandler.GiveStatic(attacker, victim, moveUsed, finalDamage);
            _moveUsageHandler.OnMoveHit -= SimulateStatic;
        }
    }
    private void AttackFirst()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("enemy should be attacked",
                () => enemy.pokemon.hp < enemy.pokemon.maxHp),
            new("Enemy's static ability should paralyze player",
                ()=> player.pokemon.statusEffect == StatusEffect.Paralysis)
        });
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        testingHandler.LogMessage($"Health of enemy: {enemy.pokemon.hp}" +
                                  $"/{enemy.pokemon.maxHp}",TestLogType.Health);

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
            //add extra logic here
            _moveUsageHandler.OnMoveHit -= SimulateStatic;
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

