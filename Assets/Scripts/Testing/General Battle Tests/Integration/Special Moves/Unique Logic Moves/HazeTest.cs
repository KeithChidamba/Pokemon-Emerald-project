using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class HazeTest : BattleBasedTest
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
        testName = "Haze Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //bulk up -> tackle
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        //tailwhip -> haze
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(1,1));
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
            enemy.Pokemon.moveSet[enemyMoveIndex].isSureHit = true;
            //haze must happen last for test case
            enemy.Pokemon.moveSet[enemyMoveIndex].priority = -5;
            _battleHandler.UseMove(enemy.Pokemon.moveSet[enemyMoveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);

        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must have attack buff",
                () => player.Pokemon.attack > player.statData.attack),
                new ("Player must have defense buff",
                () =>   player.Pokemon.defense > player.statData.defense),
            new("Player must have stat modifiers",
                () => player.Pokemon.statModifiers.Count > 0)
        });
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player must have no stat modifiers",
                () => player.Pokemon.statModifiers.Count == 0),
            new ("Player must have no defense buff",
                () =>  Mathf.FloorToInt(player.Pokemon.defense) == Mathf.FloorToInt(player.statData.defense)),
            new ("Player must have no attack buff",
                () =>   Mathf.FloorToInt(player.Pokemon.attack) == Mathf.FloorToInt(player.statData.attack)),
            
            new("Enemy must have no defense reduction",
                () => Mathf.FloorToInt(enemy.Pokemon.defense) == Mathf.FloorToInt(enemy.statData.defense)),
            new("Enemy must have no stat modifiers",
                () => enemy.Pokemon.statModifiers.Count == 0)
        });
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

