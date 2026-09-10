using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BellyDrumTest : BattleBasedTest
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
        testName = "Belly Drum Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        //use belly drum, should fail
        _sequencer.AddAction(AttackWithLowHp);
        //use belly drum, should work
        _sequencer.AddAction(()=>_sequencer.UseMove());
    }

    private void AttackWithLowHp()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        player.pokemon.hp = 1;
        player.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();
    }
    
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase("Belly drum should fail[No attack buffs]",
            () =>  player.pokemon.statModifiers.Count == 0);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("belly drum should buff attack",()=>player.pokemon.attack > player.statData.attack),
            new("Player should have stat modifiers",()=>player.pokemon.statModifiers.Count>0),
            new("belly drum should half player's health",
                ()=>player.pokemon.hp <= Mathf.FloorToInt(player.pokemon.maxHp / 2f)),
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

