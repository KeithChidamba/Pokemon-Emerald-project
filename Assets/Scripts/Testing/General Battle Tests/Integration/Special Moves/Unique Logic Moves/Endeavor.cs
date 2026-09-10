using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class Endeavor : BattleBasedTest
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
        testName = "Endeavor";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        //Endeavor -> tailwhip
        _sequencer.AddAction(()=>SetHealthForEndeavorEffect(1f,0.65f));
        _sequencer.AddAction(()=>SetHealthForEndeavorEffect(0.45f,0.85f));
    }

    private void SetHealthForEndeavorEffect(float playerHealthRatio, float enemyHealthRatio)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        player.pokemon.maxHp = enemy.pokemon.maxHp;
        
        enemy.pokemon.hp = Mathf.FloorToInt(enemy.pokemon.maxHp * enemyHealthRatio);
        player.pokemon.hp = Mathf.FloorToInt(player.pokemon.maxHp * playerHealthRatio);
        
        _sequencer.UseMove();
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Endeavor should fail,Player's health should be unchanged",
                ()=>  player.pokemon.hp >= player.pokemon.maxHp),
            new("Enemy's health should be unchanged",
                ()=> (int)enemy.pokemon.hp == Mathf.FloorToInt(enemy.pokemon.maxHp * .65f)),
        });
        
        //endeavor = victim.hp - attacker.hp
        //on the second action, enemy was given full hp
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Endeavor should work, Player's health should be unchanged",
                ()=> (int)player.pokemon.hp ==
                     Mathf.FloorToInt(player.pokemon.maxHp * .45f)),
            new("Endeavor should work, Enemy's health should decrease and match player's health",
                ()=> (int)enemy.pokemon.hp == (int)player.pokemon.hp),
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

