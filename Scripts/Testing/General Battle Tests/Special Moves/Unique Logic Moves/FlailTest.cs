using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
 
public class FlailTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private MoveSequenceHandler _moveUsageHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    private List<float> _moveDamageList = new();
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _moveUsageHandler = container.Resolve<MoveSequenceHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Flail Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        List<(int hpLevel, float damage)> damagePerLevel = new()
        {
            (32, 200f), (16, 150f), (8, 100f), (4, 80f), (2, 40f)
        };
        for (int i = 0; i < damagePerLevel.Count; i++)
        {
            var hpLevel = damagePerLevel[i].hpLevel;
            
            //flail -> tailwhip
            _sequencer.AddAction(() =>
                ForceEnemyMoveAndAttack(0, 0, hpLevel)
            );
        }
        _moveUsageHandler.OnMoveHit += TrackDamage;
    }

    private void TrackDamage(BattleParticipant attacker,BattleParticipant victim,Move move)
    {
        if(attacker.participantKey==BattleParticipantKey.Player)
        {
            _moveDamageList.Add(move.moveDamage);
        }
    }
    private void ForceEnemyMoveAndAttack(int moveIndex,int enemyMoveIndex, float hpLevel)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        enemy.pokemonTrainerAI.SetBehavior(BehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        //make damage test case more reliable
        player.pokemon.critChance = 0;
        
        player.pokemon.hp = Mathf.FloorToInt(player.pokemon.maxHp / hpLevel);
        
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
        List<(int hpLevel, float damage)> damagePerLevel = new()
        {
            (32, 200f), (16, 150f), (8, 100f), (4, 80f), (2, 40f)
        };
        for (int i = 0; i < damagePerLevel.Count; i++)
        {
            var currentCheck = i;
            var currentLevel = damagePerLevel[currentCheck];
            _testCaseHandler.AddTestCase($"Move damage must match hp Level {currentLevel.hpLevel} " +
                                         $"and damage [{currentLevel.damage}]",
                () => Mathf.FloorToInt(_moveDamageList[currentCheck])
                    == Mathf.FloorToInt(currentLevel.damage));
        }
        
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
            testingHandler.LogMessage($"Health of player: {player.pokemon.hp}" +
                                      $"/{player.pokemon.maxHp}",TestLogType.Health);
            testingHandler.LogMessage($"Flail damage ({_moveDamageList[^1]})", TestLogType.Information);
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

