using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class StatChangeApplicationTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    private BattleOperations _battleOperations;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;
    
    
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        _battleOperations = container.Resolve<BattleOperations>();
        _battleHandler = container.Resolve<BattleHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Stat Change Application Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(AttackFirst);
        _sequencer.AddAction(OnlyLetEnemyLowerDefense);
        _sequencer.AddAction(OnlyLetEnemyLowerAttack);
    }
    private void AttackFirst()
    {
        //don't allow enemy attack
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(()=>
            _turnBasedCombatHandler.SaveEmptyTurn(BattleParticipantKey.Enemy));
        
        var currentParticipant = _battleHandler.GetCurrentParticipant();
        currentParticipant.pokemon.moveSet[0].priority = 100;
        _sequencer.UseMove();//bulk up, increase attack and defense
    }
    private void OnlyLetEnemyLowerDefense()
    {
        ForceEnemyMove(0);//leer, lower defense
        _turnBasedCombatHandler.SaveEmptyTurn(BattleParticipantKey.Player);
    }
    private void OnlyLetEnemyLowerAttack()
    {
        ForceEnemyMove(1);//growl, lower attack
        _turnBasedCombatHandler.SaveEmptyTurn(BattleParticipantKey.Player);
    }
    private void ForceEnemyMove(int moveIndex)
    {
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
        enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
        enemy.pokemonTrainerAI.AssignBehaviorAction(UseSpecificMove);
        return;
        void UseSpecificMove()
        {
            _battleHandler.UseMove(enemy.pokemon.moveSet[moveIndex], enemy, BattleParticipantKey.Player);
        }
    }
    public override IEnumerator BeginTest()
    {
        _battleOperations.OnStatChangeApplied += AwaitStatChangeAddition;
        
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        
        _testCaseHandler.AddTestCase(new List<TestCaseCondition>
        {
            new("Player has higher Attack",
                ()=> player.pokemon.attack > player.statData.attack),
            new("Player has higher Defense",
                ()=>  player.pokemon.defense > player.statData.defense)
        });
        
        _testCaseHandler.AddTestCase("Player has regular Defense",
            () => (int)player.pokemon.defense == (int)player.statData.defense);
        
        _testCaseHandler.AddTestCase("Player has regular Attack",
            () => (int)player.pokemon.attack == (int)player.statData.attack);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
    private void AwaitStatChangeAddition(StatChangeOperationData operationData)
    {
        if (operationData.statChangeData.receiver.participantKey != BattleParticipantKey.Player)
        {
            return;
        }
        testingHandler.LogMessage($"The {operationData.finalStatData.statName} stat is at stage {operationData.finalStatData.stage}" +
                                  $" and is affecting {operationData.statChangeData.receiver.pokemon.pokemonName}"
            ,TestLogType.Information);
    }
    protected override void DetermineSuccess()
    {
        _testCaseHandler.HandleCurrentTestCase(CheckTestEnd,TestCaseFailed);
        return;
        void CheckTestEnd()
        {
            if (_sequencer.SequenceComplete())
            {
                _battleOperations.OnStatChangeApplied -= AwaitStatChangeAddition;
                EndTest(true);
            }
        }
        void TestCaseFailed()
        {
            _battleOperations.OnStatChangeApplied -= AwaitStatChangeAddition;
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

