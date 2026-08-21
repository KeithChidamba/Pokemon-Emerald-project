using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuryCutter : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    private List<float> _previousDamageList = new();
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Fury Cutter";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //fury cutter -> tailwhip
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        _sequencer.AddAction(()=>ForceEnemyMoveAndAttack(0,0));
        
        _moveUsageHandler.OnMoveHit += TrackDamage;
    }

    private void TrackDamage(BattleParticipant attacker,BattleParticipant victim,Move move)
    {
        if(attacker.participantKey == BattleParticipantKey.Player)
        {
            _previousDamageList.Add(move.moveDamage);
        }
    }
    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        //make damage test case more reliable
        player.pokemon.critChance = 0;
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
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        for (int i = 1; i < 5; i++)
        {
            int repetitionCount = i;
            _testCaseHandler.AddTestCase(i,$"Fury Cutter damage increased, count({i})",
                () => _previousDamageList[^1] > _previousDamageList[^2]
                      && player.previousMoveData.move.moveName == NameDB.GetMoveName(MoveName.FuryCutter)
                      && player.previousMoveData.numRepetitions == repetitionCount);
        }
        _testCaseHandler.AddTestCase(5,$"Fury Cutter damage should remain the same",
            () => Mathf.FloorToInt( _previousDamageList[^1])
                  == Mathf.FloorToInt( _previousDamageList[^2])
                  && player.previousMoveData.move.moveName == NameDB.GetMoveName(MoveName.FuryCutter)
                  && player.previousMoveData.numRepetitions == 5);
        
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
        else
        {
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            testingHandler.LogMessage($"Fury Cutter damage ({_previousDamageList[^1]}), " +
                                      $"reps({player.previousMoveData.numRepetitions})", TestLogType.Information);
        }
        return;
        void CheckTestEnd()
        {
            
            if (_sequencer.SequenceComplete())
            {
                _moveUsageHandler.OnMoveHit -= TrackDamage;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _moveUsageHandler.OnMoveHit -= TrackDamage;
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

