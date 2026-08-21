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
            enemy.pokemon.moveSet[enemyMoveIndex].isSureHit = true;
            //haze must happen last for test case
            enemy.pokemon.moveSet[enemyMoveIndex].priority = -5;
            _battleHandler.UseMove(enemy.pokemon.moveSet[enemyMoveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);

        _testCaseHandler.AddTestCase("Player must have stat buffs",
            () => player.pokemon.attack > player.statData.attack &&
            player.pokemon.defense > player.statData.defense
            && player.pokemon.statModifiers.Count > 0);
        
        _testCaseHandler.AddTestCase("Player must have no stat buff",
            () => player.pokemon.attack <= player.statData.attack &&
                  player.pokemon.defense <= player.statData.defense
                  && player.pokemon.statModifiers.Count == 0
                  
                  && enemy.pokemon.defense >= enemy.statData.defense
                  && enemy.pokemon.statModifiers.Count == 0);
        
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

