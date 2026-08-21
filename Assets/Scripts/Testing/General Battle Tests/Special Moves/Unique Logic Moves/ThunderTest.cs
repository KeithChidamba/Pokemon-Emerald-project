using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class ThunderTest : BattleBasedTest
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
        testName = "Thunder Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //Thunder -> Rain, sure hit is part of test case, so remove modification
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0,false));
        
        //Thunder -> Sunlight, needs sure hit modification
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,1,true));
        
        _moveUsageHandler.OnMoveHit += CheckThunderMoveState;
    }
    private void CheckThunderMoveState(BattleParticipant attacker,BattleParticipant victim,Move moveUsed)
    {
        if(attacker.participantKey==BattleParticipantKey.Player)
        {
            var caseIndex = _sequencer.GetTestCaseIndex();
            if (caseIndex == 0)
            {
                _testCaseHandler.AddTestCase("Rain must ensure thunder hits",
                    () => moveUsed.isSureHit);
            }
            else
            {
                _testCaseHandler.AddTestCase("Sunlight must lower accuracy of thunder",
                    () => moveUsed.moveAccuracy <= 50f);
            }
        }
    }
    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex,bool isSureHit)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        
        _sequencer.UseMove(moveIndex,isSureHit);
        return;
        void UseSpecificMove()
        {
            enemy.pokemon.moveSet[enemyMoveIndex].priority = 100;
            enemy.pokemon.moveSet[enemyMoveIndex].statusChance = 100;
            enemy.pokemon.moveSet[enemyMoveIndex].isSureHit = true;
            _battleHandler.UseMove(enemy.pokemon.moveSet[enemyMoveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
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
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnMoveHit -= CheckThunderMoveState;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnMoveHit -= CheckThunderMoveState;
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

