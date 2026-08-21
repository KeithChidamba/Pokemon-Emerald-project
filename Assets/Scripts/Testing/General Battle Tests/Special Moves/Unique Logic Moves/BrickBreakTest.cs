using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class BrickBreakTest : BattleBasedTest
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
        testName = "Brick Break Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //tackle -> reflect
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        //tackle -> light-screen
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,1));
        //brick break -> tailwhip
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(1,2));
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
        
        _testCaseHandler.AddTestCase("Enemy must have physical barrier from reflect",
            () => enemy.barriers.Any(b=>b.barrierName == NameDB.GetMoveName(MoveName.Reflect)));
        
        _testCaseHandler.AddTestCase("Enemy must have special barrier from light screen",
            () => enemy.barriers.Any(b=>b.barrierName == NameDB.GetMoveName(MoveName.LightScreen)));
        
        _testCaseHandler.AddTestCase("Enemy must have no barriers",
            () => enemy.barriers.Count==0);
        
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

