using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class FalseSwipeTest : BattleBasedTest
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
        testName = "False Swipe Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //false swip -> tailwhip
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
    }

    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        
        _sequencer.UseMove(moveIndex);
        return;
        void UseSpecificMove()
        {
            //modified for test case reliability
            enemy.pokemon.moveSet[enemyMoveIndex].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[enemyMoveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase("Enemy is weak enough to faint but wont because of move logic, must be at 1hp",
            () => Mathf.FloorToInt(enemy.pokemon.hp) == 1);
        
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

