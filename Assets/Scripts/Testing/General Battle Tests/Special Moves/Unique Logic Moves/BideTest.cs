using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public class BideTest : BattleBasedTest
{
    private BattleHandler _battleHandler;
    private TurnBasedCombatHandler _turnBasedCombatHandler;
    
    private MoveTestActionSequencer _sequencer;
    private TestCaseHandler _testCaseHandler;

    private float _currentBideDamageStore;
    public override void Inject(ServiceContainer serviceContainer)
    {
        container = serviceContainer;
        _battleHandler = container.Resolve<BattleHandler>();
        _turnBasedCombatHandler = container.Resolve<TurnBasedCombatHandler>();
        
        _sequencer = new MoveTestActionSequencer(container);
        _testCaseHandler = new TestCaseHandler(testingHandler,_sequencer);
        testName = "Bide Test";
        
        testExitCondition = TestCompletionCondition.EndManually;
        
        _sequencer.AddAction(()=>
        {
            //Bide , but make enemy uses tackle
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy);
            enemy.pokemonTrainerAI.SetBehavior(BattleAiBehaviorMode.Controlled);
            enemy.pokemonTrainerAI.AssignBehaviorAction(() =>
            {
                //Tackle
                enemy.pokemon.moveSet[0].isSureHit = true;
                _battleHandler.UseMove(enemy.pokemon.moveSet[0], enemy, BattleParticipantKey.Player);
            });
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.moveSet[0].priority = 100;
            _sequencer.UseMove();
        });
        _sequencer.AddAction(()=>{});//cooldown turn buffer
        _sequencer.AddAction(()=>{});//cooldown turn buffer
        _sequencer.AddAction(()=>{});//cooldown turn buffer
        _sequencer.AddAction(()=>_sequencer.UseMove(1));
    }
    public override IEnumerator BeginTest()
    {
        var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
        var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy); 
        
        _testCaseHandler.AddTestCase(0,"Bide should be activated",
            () =>NameDB.ParseMoveName(player.currentCoolDown.turnData.move.moveName) == MoveName.Bide);
        
        _testCaseHandler.AddTestCase(3,"Bide should hit enemy",
            () => enemy.pokemon.hp < enemy.pokemon.maxHp);
        
        _testCaseHandler.AddTestCase(4,"Player cooldown down should be over, and player should use tailwhip",
            () =>NameDB.ParseMoveName(player.previousMoveData.move.moveName) == MoveName.TailWhip);
        
        yield return HandleBattleState();
        onTestResult.Invoke();
    }
  
    protected override void DetermineSuccess()
    {
        Debug.Log("case: " + _sequencer.GetTestCaseIndex());
        var caseExists = _testCaseHandler.CheckForCurrentTestCase(CheckTestEnd,TestCaseFailed);
        if (!caseExists)
        {
            CheckTestEnd();
        }
        return;
        void CheckTestEnd()
        {
            var enemy = _battleHandler.GetParticipant(BattleParticipantKey.Enemy); 
            var player = _battleHandler.GetParticipant(BattleParticipantKey.Player);
            player.pokemon.hp = player.pokemon.maxHp;
            enemy.pokemon.hp = enemy.pokemon.maxHp;
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

